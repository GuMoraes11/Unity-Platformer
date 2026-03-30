using UnityEngine;
using TarodevController;

/// <summary>
/// Purple platform: when active, reduces or halts the player's horizontal movement
/// while leaving vertical movement (jump/fall/gravity) completely unaffected.
///
/// Integrates with the existing <see cref="ISpeedModifier"/> contract consumed by
/// <see cref="PlayerController"/>. The controller discovers this modifier via
/// OnTriggerEnter2D/Exit2D (PlayerController.cs:L1052-L1061) and applies it in
/// CalculateExternalModifiers (PlayerController.cs:L1064-L1074).
///
/// The modifier vector is additive to Vector2.one, then the sum is used as a
/// per-component multiplier on the final velocity (PlayerController.cs:L1003).
/// Therefore:
///   Halt mode        → Modifier = (-1, 0)  → x multiplier becomes 0, y stays 1.
///   Slow mode        → Modifier = (-(1-slowPercent), 0) → x multiplier = slowPercent, y stays 1.
///   Freeze all mode  → Modifier = (-1, -1) → both axes zeroed; player cannot move at all.
///
/// When freezeAllMovement is off, the Y component is always 0 so vertical velocity
/// is never affected. When on, both axes are zeroed and InAir also returns true so
/// the freeze persists even while airborne.
///
/// Optional color gating follows the same pattern as Orange.cs: queries
/// <see cref="ColorManager.IsPlatformGroupCurrentlyActive"/> each frame.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PurplePlatform : MonoBehaviour, ISpeedModifier
{
    // ── Inspector ───────────────────────────────────────────────────────

    [Header("Activation")]
    [Tooltip("Master toggle. When false the modifier has no effect.")]
    public bool isActive = true;

    [Header("Movement Effect")]
    [Tooltip("When true, ALL movement (horizontal AND vertical) is frozen. Overrides effectMode.")]
    public bool freezeAllMovement = false;

    public HorizontalEffectMode effectMode = HorizontalEffectMode.Halt;

    [Tooltip("Only used when effectMode = SlowPercent. 0.3 means the player retains 30% horizontal speed.")]
    [Range(0f, 1f)]
    public float slowPercent = 0.3f;

    [Header("Color Gating (optional)")]
    [Tooltip("If true, the platform effect is only active when its color group is the current active color.")]
    public bool gateByActiveColor = false;

    [Tooltip("Color name to match against ColorManager.colorPlatforms entries.")]
    public string colorName = "Purple";

    [Tooltip("Auto-found via FindObjectOfType if left null.")]
    public ColorManager colorManager;

    [Header("References (auto-populated)")]
    [Tooltip("The trigger collider used for ISpeedModifier detection. Auto-fetched if null.")]
    public Collider2D platformCollider;

    [Header("Debug")]
    public bool drawGizmos = false;

    // ── ISpeedModifier implementation ───────────────────────────────────

    /// <summary>
    /// The modifier applies when the player is grounded on the platform.
    /// This matches the PlayerController check at L1069:
    ///   if ((modifier.OnGround && _grounded) || (modifier.InAir && !_grounded))
    /// </summary>
    public bool OnGround => true;

    /// <summary>
    /// When freezeAllMovement is enabled, the modifier must also apply while
    /// airborne so the player cannot jump out of the freeze. Otherwise, only
    /// grounded application — the player can jump freely.
    /// </summary>
    public bool InAir => freezeAllMovement && IsEffectActive();

    /// <summary>
    /// Returns the additive modifier vector. The PlayerController accumulates
    /// modifiers as: frameSpeedModifier = Vector2.one + Σ(modifier.Modifier).
    /// The result is then used as a per-component velocity multiplier.
    ///
    /// When freezeAllMovement is false, only the X component is touched; Y stays 0
    /// so vertical velocity is unaffected.
    /// When freezeAllMovement is true, both X and Y are zeroed — full freeze.
    ///
    /// When the platform is inactive (master toggle off, or color-gated off),
    /// we return Vector2.zero so the multiplier stays at (1,1) — no effect.
    /// </summary>
    public Vector2 Modifier
    {
        get
        {
            if (!IsEffectActive())
                return Vector2.zero;

            // Full freeze: zero both axes → additive contribution = (-1, -1)
            if (freezeAllMovement)
                return new Vector2(-1f, -1f);

            return effectMode switch
            {
                // Halt: x multiplier should become 0 → additive contribution = -1
                HorizontalEffectMode.Halt => new Vector2(-1f, 0f),

                // SlowPercent: x multiplier should become slowPercent
                // → additive contribution = -(1 - slowPercent)
                HorizontalEffectMode.SlowPercent => new Vector2(-(1f - slowPercent), 0f),

                _ => Vector2.zero
            };
        }
    }

    // ── Enums ───────────────────────────────────────────────────────────

    public enum HorizontalEffectMode
    {
        /// <summary>Player horizontal input/movement is fully prevented.</summary>
        Halt,

        /// <summary>Player horizontal movement is scaled by <see cref="slowPercent"/>.</summary>
        SlowPercent
    }

    // ── Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-fetch collider if not assigned
        if (platformCollider == null)
            platformCollider = GetComponent<Collider2D>();

        // The collider MUST be a trigger for PlayerController's OnTriggerEnter2D to detect it.
        // If the designer placed a solid collider for ground contact, we need a separate
        // child trigger. Warn but don't force-override — the designer may have set it up correctly.
        if (platformCollider != null && !platformCollider.isTrigger)
        {
            Debug.LogWarning(
                $"[PurplePlatform] '{name}': The collider assigned to platformCollider is not a trigger. " +
                "PlayerController discovers ISpeedModifier via OnTriggerEnter2D, so a trigger collider " +
                "is required. Consider adding a child GameObject with a trigger collider, or set " +
                "isTrigger = true on this collider if ground contact is handled by a separate collider.",
                this);
        }

        // Auto-find ColorManager if color gating is enabled but no reference assigned
        if (gateByActiveColor && colorManager == null)
        {
            colorManager = FindObjectOfType<ColorManager>();
            if (colorManager == null)
            {
                Debug.LogWarning(
                    $"[PurplePlatform] '{name}': gateByActiveColor is enabled but no ColorManager " +
                    "was found in the scene. Color gating will be disabled.",
                    this);
                gateByActiveColor = false;
            }
        }
    }

    // ── Internal ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the platform's horizontal effect should currently apply.
    /// Checks the master toggle and optional color gating.
    /// </summary>
    private bool IsEffectActive()
    {
        if (!isActive)
            return false;

        if (gateByActiveColor)
            return IsColorActive();

        return true;
    }

    /// <summary>
    /// Queries ColorManager to determine if this platform's color group is currently active.
    /// Follows the same pattern as Orange.cs (iterates colorPlatforms, matches by name,
    /// checks IsPlatformGroupCurrentlyActive).
    ///
    /// Evidence: Assets/Scripts/Colors/ColorManager.cs:L23 (colorPlatforms list),
    ///           Assets/Scripts/Colors/ColorManager.cs:L126-L130 (IsPlatformGroupCurrentlyActive).
    /// </summary>
    private bool IsColorActive()
    {
        if (colorManager == null)
            return true; // Fail-open: no manager means always active

        foreach (var cp in colorManager.colorPlatforms)
        {
            if (cp.platformGroup == null) continue;

            // Match by color name (case-insensitive), same approach as Orange.cs
            if (string.Equals(cp.colorName, colorName, System.StringComparison.OrdinalIgnoreCase))
            {
                return colorManager.IsPlatformGroupCurrentlyActive(cp.platformGroup);
            }
        }

        // No matching color group found — default to active
        return true;
    }

    // ── Gizmos ──────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (platformCollider == null) return;

        Gizmos.color = IsEffectActive()
            ? new Color(0.6f, 0.2f, 0.8f, 0.35f)  // Purple, semi-transparent
            : new Color(0.4f, 0.4f, 0.4f, 0.2f);  // Gray when inactive

        Gizmos.DrawCube(platformCollider.bounds.center, platformCollider.bounds.size);

        // Wireframe outline
        Gizmos.color = new Color(0.6f, 0.2f, 0.8f, 0.8f);
        Gizmos.DrawWireCube(platformCollider.bounds.center, platformCollider.bounds.size);
    }
#endif
}

// ════════════════════════════════════════════════════════════════════════
// Test checklist
// ════════════════════════════════════════════════════════════════════════
//
// 1. Walking on platform halts/slows horizontally:
//    - Place PurplePlatform with effectMode = Halt. Walk onto it.
//    - Expected: player cannot move horizontally while grounded on the trigger.
//    - Switch to SlowPercent (0.3). Walk onto it.
//    - Expected: player moves at 30% horizontal speed.
//
// 2. Jumping unaffected (vertical velocity unchanged) — freezeAllMovement OFF:
//    - Stand on PurplePlatform (freezeAllMovement = false), press jump.
//    - Expected: jump height and fall speed are identical to normal ground.
//    - Verify: InAir = false, so modifier is NOT applied while airborne.
//
// 3. Freeze all movement — freezeAllMovement ON:
//    - Enable freezeAllMovement in inspector. Stand on the platform.
//    - Expected: player cannot move horizontally OR vertically (no jump, no fall).
//    - InAir returns true, so the modifier persists even if the player somehow
//      becomes airborne. Modifier = (-1, -1) → velocity multiplier = (0, 0).
//    - Disable freezeAllMovement → normal effectMode behavior resumes.
//
// 4. Stepping off returns movement to normal:
//    - Walk off the edge of the PurplePlatform trigger.
//    - Expected: OnTriggerExit2D fires in PlayerController, removing this
//      ISpeedModifier from _modifiers. SmoothDamp returns multiplier to (1,1).
//
// 5. Platform moving carries player horizontally (stick):
//    - Attach PurplePlatform to a GameObject that also has MovingPlatform.
//    - Expected: IPhysicsMover handles carry via FramePositionDelta (separate
//      system). PurplePlatform's ISpeedModifier reduces/halts the player's OWN
//      input-driven movement, but the platform carry from IPhysicsMover still
//      applies (transient velocity is added separately at PlayerController.cs:L1003).
//
// 6. Color gating (if enabled) correctly disables behavior:
//    - Set gateByActiveColor = true, colorName = "Purple".
//    - Cycle colors via Q/E. When Purple is not the active color, Modifier
//      returns Vector2.zero → no horizontal effect.
//    - When Purple becomes active, effect resumes.
//
// ════════════════════════════════════════════════════════════════════════

