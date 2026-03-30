using System.Collections.Generic;
using UnityEngine;
using TarodevController;

// ════════════════════════════════════════════════════════════════════════
// Setup hierarchy:
//
//   GreenPadRoot                (GreenPlatform, optional SpriteRenderer)
//   ├── OuterZone               (BoxCollider2D [trigger] + GreenPadZoneRelay [Outer])
//   └── InnerZone               (BoxCollider2D [trigger] + GreenPadZoneRelay [Inner])
//
// - Root has NO solid collider. Entirely trigger-based.
// - OuterZone = main bounce detection area (required).
// - InnerZone = bonus region inside the outer (optional).
// - Both zones must be on a NON-GROUND trigger layer.
// - GreenPadZoneRelay on each child forwards trigger events to this script.
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Green bounce platform — pure trigger-based launcher volume.
///
/// Two-zone design driven by <see cref="GreenPadZoneRelay"/> children:
///   OUTER zone: on first enter, queues a launch for the next FixedUpdate.
///   INNER zone: tracks whether the player is inside at launch time for bonus.
///
/// Launch is deferred to FixedUpdate so that both outer and inner enter events
/// from the same physics step are settled before the launch velocity is computed.
/// This eliminates event-order sensitivity between the two zones.
///
/// Per-player state (keyed by root Transform):
///   outerCount     — ref-counted collider overlaps with outer zone
///   innerCount     — ref-counted collider overlaps with inner zone
///   launchQueued   — set on outer 0→1 transition, consumed in FixedUpdate
///   launchApplied  — prevents re-launch while still inside outer
///
/// On outer exit (outerCount reaches 0): all state is cleared (re-arms).
///
/// Uses <see cref="IPlayerController.QueueExternalVelocity"/> to set an exact
/// target velocity, bypassing the normal movement pipeline for one frame.
/// </summary>
public class GreenPlatform : MonoBehaviour
{
    // ── Enums ───────────────────────────────────────────────────────────

    public enum ReflectionMode
    {
        /// <summary>(vIn.x, -vIn.y)</summary>
        ReflectVerticalOnly,
        /// <summary>(-vIn.x, -vIn.y)</summary>
        ReflectAll
    }

    public enum FacingAxis { Up, Right }

    // ── Per-player state ────────────────────────────────────────────────

    private struct PlayerState
    {
        public int outerCount;
        public int innerCount;
        public bool launchQueued;
        public bool launchApplied;
    }

    // ── Inspector ───────────────────────────────────────────────────────

    [Header("Reflection")]
    [Tooltip("How the player's velocity is reflected on launch.")]
    public ReflectionMode reflectionMode = ReflectionMode.ReflectVerticalOnly;

    [Header("Bonus")]
    [Tooltip("Which local axis of the ZONE transform is the bonus direction.")]
    public FacingAxis facingAxis = FacingAxis.Up;

    [Tooltip("Extra speed added along the zone's facing direction on launch.")]
    [Min(0f)]
    public float bonusSpeed = 0f;

    [Tooltip("If true, bonus only applies when the player is inside the inner zone at launch time.\n" +
             "If false, bonus always applies.")]
    public bool requireInnerZoneForBonus = true;

    [Header("Zones")]
    [Tooltip("Outer zone relay (bounce detection). Required.")]
    public GreenPadZoneRelay outerZone;

    [Tooltip("Inner zone relay (bonus region). Optional.")]
    public GreenPadZoneRelay innerZone;

    [Header("Color Gating")]
    [Tooltip("If true, launch only works when the platform's color group is active.")]
    public bool gateByActiveColor = false;

    [Tooltip("Color name to match against ColorManager.colorPlatforms.")]
    public string colorName = "Green";

    [Tooltip("Auto-found via FindObjectOfType if null.")]
    public ColorManager colorManager;

    [Header("Debug")]
    public bool logBounceDebug = false;
    public bool drawGizmos = false;

    // ── Runtime state ───────────────────────────────────────────────────

    private readonly Dictionary<Transform, PlayerState> _players = new();

    // Temp list to avoid allocation during FixedUpdate iteration
    private readonly List<Transform> _pendingLaunches = new();

    // ── Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (gateByActiveColor && colorManager == null)
        {
            colorManager = FindObjectOfType<ColorManager>();
            if (colorManager == null)
            {
                Debug.LogWarning(
                    $"[GreenPlatform] '{name}': gateByActiveColor enabled but no ColorManager found. " +
                    "Color gating disabled.", this);
                gateByActiveColor = false;
            }
        }

        ValidateZonesRuntime();
    }

    private void OnDisable()
    {
        _players.Clear();
        _pendingLaunches.Clear();
    }

    // ── FixedUpdate: process queued launches ────────────────────────────

    private void FixedUpdate()
    {
        if (_players.Count == 0) return;

        // Collect players that need launching
        _pendingLaunches.Clear();
        foreach (var kvp in _players)
        {
            var s = kvp.Value;
            if (s.launchQueued && !s.launchApplied && s.outerCount > 0)
                _pendingLaunches.Add(kvp.Key);
        }

        if (_pendingLaunches.Count == 0) return;

        // Color gate check (once per frame, not per player)
        if (gateByActiveColor && !IsColorActive())
        {
            // Clear queued flags without launching
            for (int i = 0; i < _pendingLaunches.Count; i++)
            {
                var root = _pendingLaunches[i];
                var s = _players[root];
                s.launchQueued = false;
                _players[root] = s;
            }
            return;
        }

        // Process launches
        for (int i = 0; i < _pendingLaunches.Count; i++)
        {
            var root = _pendingLaunches[i];
            if (!_players.TryGetValue(root, out var state)) continue;

            var pc = root.GetComponentInChildren<IPlayerController>();
            if (pc == null)
            {
                state.launchQueued = false;
                _players[root] = state;
                continue;
            }

            // Compute exact target velocity (not a force — an absolute velocity).
            Vector2 vIn = pc.Velocity;
            Vector2 target = ComputeReflection(vIn);

            // Bonus check
            bool applyBonus = false;
            if (bonusSpeed > 0f)
            {
                if (!requireInnerZoneForBonus)
                    applyBonus = true;
                else if (state.innerCount > 0)
                    applyBonus = true;
            }

            if (applyBonus)
            {
                // Bonus direction from the zone that provides it
                Transform bonusZoneTransform = (state.innerCount > 0 && innerZone != null)
                    ? innerZone.transform
                    : (outerZone != null ? outerZone.transform : transform);

                Vector2 bonusDir = GetFacingDirection(bonusZoneTransform);
                target += bonusDir * bonusSpeed;
            }

            // Apply launch via exact velocity override. This bypasses the normal
            // movement pipeline for one frame so grounding/gravity cannot distort it.
            pc.QueueExternalVelocity(target);

            state.launchQueued = false;
            state.launchApplied = true;
            _players[root] = state;

            if (logBounceDebug)
            {
                Debug.Log($"[GreenPlatform] '{name}' LAUNCH | root={root.name} " +
                          $"mode={reflectionMode} bonus={applyBonus} vIn={vIn} target={target}", this);
            }
        }
    }

    // ── Zone notifications (called by GreenPadZoneRelay) ────────────────

    /// <summary>Called by <see cref="GreenPadZoneRelay"/> on trigger enter.</summary>
    public void NotifyZoneEnter(Transform playerRoot, GreenPadZoneRelay.ZoneType zone)
    {
        _players.TryGetValue(playerRoot, out var state);

        if (zone == GreenPadZoneRelay.ZoneType.Outer)
        {
            bool wasOutside = state.outerCount == 0;
            state.outerCount++;

            if (wasOutside)
            {
                // First outer enter: queue launch, reset applied flag
                state.launchQueued = true;
                state.launchApplied = false;
            }
        }
        else // Inner
        {
            state.innerCount++;
        }

        _players[playerRoot] = state;
    }

    /// <summary>Called by <see cref="GreenPadZoneRelay"/> on trigger exit.</summary>
    public void NotifyZoneExit(Transform playerRoot, GreenPadZoneRelay.ZoneType zone)
    {
        if (!_players.TryGetValue(playerRoot, out var state)) return;

        if (zone == GreenPadZoneRelay.ZoneType.Outer)
        {
            state.outerCount--;
            if (state.outerCount <= 0)
            {
                // Fully exited outer: clear all state (re-arm)
                _players.Remove(playerRoot);
                return;
            }
        }
        else // Inner
        {
            state.innerCount = Mathf.Max(0, state.innerCount - 1);
        }

        _players[playerRoot] = state;
    }

    // ── Reflection ──────────────────────────────────────────────────────

    private Vector2 ComputeReflection(Vector2 vIn)
    {
        return reflectionMode switch
        {
            ReflectionMode.ReflectVerticalOnly => new Vector2(vIn.x, -vIn.y),
            ReflectionMode.ReflectAll          => new Vector2(-vIn.x, -vIn.y),
            _                                  => new Vector2(vIn.x, -vIn.y)
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Vector2 GetFacingDirection(Transform zoneTransform)
    {
        return facingAxis switch
        {
            FacingAxis.Up    => ((Vector2)zoneTransform.up).normalized,
            FacingAxis.Right => ((Vector2)zoneTransform.right).normalized,
            _                => ((Vector2)zoneTransform.up).normalized
        };
    }

    private bool IsColorActive()
    {
        if (colorManager == null) return true;

        foreach (var cp in colorManager.colorPlatforms)
        {
            if (cp.platformGroup == null) continue;
            if (string.Equals(cp.colorName, colorName, System.StringComparison.OrdinalIgnoreCase))
                return colorManager.IsPlatformGroupCurrentlyActive(cp.platformGroup);
        }

        return true;
    }

    // ── Validation ──────────────────────────────────────────────────────

    private void ValidateZone(GreenPadZoneRelay relay, string label, LayerMask? playerCollisionLayers = null)
    {
        if (relay == null)
        {
            if (label == "outerZone")
                Debug.LogWarning($"[GreenPlatform] '{name}': {label} is not assigned. Platform will not launch.", this);
            return;
        }

        var col = relay.GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[GreenPlatform] '{name}': {label} '{relay.name}' has no Collider2D.", this);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[GreenPlatform] '{name}': {label} '{relay.name}' collider is NOT a trigger. Set isTrigger=true.", this);
        }

        int zoneLayer = relay.gameObject.layer;
        string zonelayerName = LayerMask.LayerToName(zoneLayer);

        // Warn for well-known layers that are included in PlayerStats.CollisionLayers
        // (Default=0, Ground=3, Climbable=8, Ladders=9 per the current project setup).
        // Trigger zones on these layers can cause false grounding in PlayerController:
        // the ground raycast hits the trigger, ToggleGrounded(true) fires, vertical
        // velocity is zeroed, and the bounce produces no upward force.
        string[] dangerousLayers = { "Default", "Ground", "Climbable", "Ladders" };
        foreach (var dangerous in dangerousLayers)
        {
            if (zoneLayer == LayerMask.NameToLayer(dangerous))
            {
                Debug.LogWarning(
                    $"[GreenPlatform] '{name}': {label} '{relay.name}' is on layer '{dangerous}' (index {zoneLayer}). " +
                    "This layer is included in the player's collision/ground detection mask (PlayerStats.CollisionLayers). " +
                    "Trigger zones on these layers cause false grounding — the ground raycast hits the trigger, " +
                    "zeros vertical velocity, and prevents bouncing. " +
                    "Move this zone to a layer NOT in CollisionLayers (e.g., an unused layer like index 6).", this);
                break;
            }
        }

        // If we have the player's actual CollisionLayers mask at runtime, also check
        // against that. Catches custom layers added to the mask that aren't in the
        // hard-coded list above.
        if (playerCollisionLayers.HasValue)
        {
            int layerBit = 1 << zoneLayer;
            if ((playerCollisionLayers.Value.value & layerBit) != 0)
            {
                Debug.LogWarning(
                    $"[GreenPlatform] '{name}': {label} '{relay.name}' is on layer '{zonelayerName}' (index {zoneLayer}), " +
                    "which is included in the player's CollisionLayers mask. " +
                    "Trigger zones on collision-detection layers cause false grounding in PlayerController — " +
                    "the ground raycast hits the trigger, zeros vertical velocity, and prevents bouncing. " +
                    "Move this zone to a layer NOT in PlayerStats.CollisionLayers.", this);
            }
        }
    }

    /// <summary>
    /// Runtime validation: tries to find a PlayerController to read its
    /// CollisionLayers mask for a more precise layer check.
    /// </summary>
    private void ValidateZonesRuntime()
    {
        LayerMask? collisionLayers = null;

        // Try to find any PlayerController in the scene to read its collision mask.
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null && pc.Stats != null)
            collisionLayers = pc.Stats.CollisionLayers;

        ValidateZone(outerZone, "outerZone", collisionLayers);
        ValidateZone(innerZone, "innerZone", collisionLayers);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // In editor, we don't have a reliable PlayerController instance,
        // so validate with just the "Ground" layer name check.
        ValidateZone(outerZone, "outerZone");
        ValidateZone(innerZone, "innerZone");
    }
#endif

    // ── Gizmos ──────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (outerZone != null)
        {
            var col = outerZone.GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.2f);
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
                Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.6f);
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            Vector2 dir = GetFacingDirection(outerZone.transform);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
            Gizmos.DrawRay(outerZone.transform.position, dir * 1.5f);
        }

        if (innerZone != null)
        {
            var col = innerZone.GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(0.9f, 0.9f, 0.1f, 0.2f);
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
                Gizmos.color = new Color(0.9f, 0.9f, 0.1f, 0.5f);
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            Vector2 dir = GetFacingDirection(innerZone.transform);
            Gizmos.color = new Color(0.9f, 0.9f, 0.1f, 0.9f);
            Gizmos.DrawRay(innerZone.transform.position, dir * 1.0f);
        }
    }
#endif
}

// ════════════════════════════════════════════════════════════════════════
// Implementation note
// ════════════════════════════════════════════════════════════════════════
//
// This uses an exact velocity override (QueueExternalVelocity) instead of
// force accumulation (AddFrameForce) so launcher behavior is deterministic
// and not distorted by the normal movement pipeline (gravity, grounding,
// jump-cut multiplier, speed modifiers, etc.).
//
// ════════════════════════════════════════════════════════════════════════
// Test checklist
// ════════════════════════════════════════════════════════════════════════
//
// 1. Enter outer once → one launch:
//    - Player enters outer zone. FixedUpdate fires launch once.
//    - Enable logBounceDebug, confirm exactly one LAUNCH log per entry.
//
// 2. Stay inside outer → no repeat:
//    - launchApplied=true prevents re-launch while outerCount > 0.
//
// 3. Enter outer while also in inner → launch includes bonus:
//    - Inner zone overlaps outer. Player enters both in same physics step.
//    - Because launch is deferred to FixedUpdate, innerCount > 0 is visible.
//    - target includes bonusDir * bonusSpeed.
//
// 4. Inner/outer event order does not matter:
//    - Launch is processed in FixedUpdate AFTER all trigger callbacks.
//    - Whether inner or outer fires first, the settled state is the same.
//
// 5. Leave outer and re-enter → launch again:
//    - outerCount reaches 0 → _players.Remove clears all state.
//    - Next outer enter queues a fresh launch.
//
// 6. Zones on ground-detection layers → warning:
//    - OnValidate warns if zone is on Default/Ground/Climbable/Ladders.
//    - Awake also checks against PlayerStats.CollisionLayers mask (if a PlayerController is found).
//    - Warning explains that trigger zones on ground-detection layers cause false grounding,
//      which zeros vertical velocity and prevents bouncing.
//
// 7. No collision callbacks used:
//    - No OnCollisionEnter2D / OnCollisionExit2D anywhere.
//    - No Physics2D.IgnoreCollision.
//    - Entirely trigger-based via GreenPadZoneRelay.
//
// 8. Deterministic velocity:
//    - QueueExternalVelocity sets rb.linearVelocity to the exact computed
//      target and skips the rest of the movement pipeline for that frame.
//    - Bounce height is fully determined by ComputeReflection + bonusSpeed.
//
// ════════════════════════════════════════════════════════════════════════

