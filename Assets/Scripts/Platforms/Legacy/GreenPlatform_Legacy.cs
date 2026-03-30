using System.Collections.Generic;
using UnityEngine;
using TarodevController;

/// <summary>
/// LEGACY — preserved copy of GreenPlatform.cs before the v2 rewrite.
/// Class renamed to GreenPlatform_Legacy to avoid duplicate symbol errors.
/// Original path: Assets/Scripts/Platforms/GreenPlatform.cs
///
/// Green launch/bounce platform. Reflects the player's velocity about the
/// platform's facing axis and optionally adds bonus momentum when the player
/// is inside the boost trigger zone.
///
/// Integration:
///   Uses <see cref="IPlayerController.AddFrameForce"/> (PlayerController.cs:L73-L77,
///   L1140) to apply the bounce impulse. AddFrameForce with resetVelocity=true zeros
///   existing velocity, then the force is applied as an impulse in Move()
///   (PlayerController.cs:L877-L884) via rb.AddForce(..., ForceMode2D.Impulse).
///   This bypasses normal movement for that frame, giving a crisp launch.
///
/// Setup:
///   - This GameObject needs a SOLID (non-trigger) Collider2D for ground/wall contact.
///   - A child GameObject with <see cref="GreenPlatformBoostZone_Legacy"/> + trigger Collider2D
///     defines the bonus region. If no boost zone is assigned and requireBoostZoneForBonus
///     is false, bonus is always applied.
///
/// Multi-collider safety:
///   The player has two colliders (BoxCollider2D grounded, CapsuleCollider2D airborne)
///   that swap on state change. Cooldown is keyed by PlayerController instance ID
///   (stable identity), not by individual collider instance ID. The solid collider
///   filter (collision.otherCollider == solidCollider) prevents non-solid colliders
///   on the platform from triggering bounces. A contact normal threshold prevents
///   side/edge scrapes from bouncing.
///
/// Bounce math:
///   1. Get player velocity v and platform facing direction n.
///   2. Decompose: along = dot(v, n) * n; side = v - along.
///   3. Reflect: vReflected = -along + side  (mirror the component along n).
///   4. Ensure result points forward (positive dot with n); if not, flip.
///   5. Scale by bounceMultiplier.
///   6. If in boost zone: add n * bonusSpeed.
/// </summary>
public class GreenPlatform_Legacy : MonoBehaviour
{
    // ── Inspector ───────────────────────────────────────────────────────

    [Header("Facing")]
    [Tooltip("Which local axis is the 'launch direction'. Up = transform.up, Right = transform.right.")]
    public FacingAxis facingAxis = FacingAxis.Up;

    [Header("Bounce")]
    [Tooltip("Multiplier applied to the reflected velocity magnitude.")]
    public float bounceMultiplier = 1.0f;

    [Tooltip("Extra speed added along the facing direction when the player is in the boost zone.")]
    public float bonusSpeed = 0f;

    [Tooltip("If true, bonus speed is only applied when the player overlaps the boost trigger zone. " +
             "If false, bonus is always applied on any collision.")]
    public bool requireBoostZoneForBonus = true;

    [Header("Cooldown")]
    [Tooltip("Minimum seconds between bounces for the same player. Prevents rapid repeat bounces.")]
    public float bounceCooldown = 0.1f;

    [Header("Contact Filter")]
    [Tooltip("Minimum dot product between the contact normal and the negative facing direction " +
             "required to trigger a bounce. Prevents side/edge contacts from bouncing. " +
             "0 = any contact, 1 = perfectly head-on only. Recommended: 0.2–0.3.")]
    [Range(0f, 1f)]
    public float contactNormalThreshold = 0.25f;

    [Header("Minimum Launch Speed")]
    [Tooltip("If the reflected velocity magnitude (before bonus) is below this, use this as the " +
             "minimum launch speed along the facing direction. Prevents weak bounces from slow contacts.")]
    public float minimumBounceSpeed = 5f;

    [Header("References")]
    [Tooltip("The solid (non-trigger) collider on this platform. Auto-fetched if null.")]
    public Collider2D solidCollider;

    [Tooltip("The boost zone trigger. Assign manually or auto-found from children.")]
    public GreenPlatformBoostZone_Legacy boostZone;

    [Header("Color Gating (optional)")]
    [Tooltip("If true, the bounce only works when the platform's color group is active.")]
    public bool gateByActiveColor = false;

    [Tooltip("Color name to match against ColorManager.colorPlatforms entries.")]
    public string colorName = "Green";

    [Tooltip("Auto-found via FindObjectOfType if left null.")]
    public ColorManager colorManager;

    [Header("Debug")]
    public bool drawGizmos = false;

    // ── Enums ───────────────────────────────────────────────────────────

    public enum FacingAxis { Up, Right }

    // ── State ───────────────────────────────────────────────────────────

    private readonly Dictionary<int, float> _lastBounceTime = new();

    // ── Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (solidCollider == null)
        {
            foreach (var col in GetComponents<Collider2D>())
            {
                if (!col.isTrigger)
                {
                    solidCollider = col;
                    break;
                }
            }
        }

        if (boostZone == null)
            boostZone = GetComponentInChildren<GreenPlatformBoostZone_Legacy>();

        if (gateByActiveColor && colorManager == null)
        {
            colorManager = FindObjectOfType<ColorManager>();
            if (colorManager == null)
            {
                Debug.LogWarning(
                    $"[GreenPlatform_Legacy] '{name}': gateByActiveColor is enabled but no ColorManager " +
                    "was found in the scene. Color gating will be disabled.", this);
                gateByActiveColor = false;
            }
        }
    }

    // ── Collision ───────────────────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (solidCollider != null && collision.otherCollider != solidCollider)
            return;

        if (!collision.collider.CompareTag("Player"))
            return;

        if (gateByActiveColor && !IsColorActive())
            return;

        if (collision.contactCount > 0)
        {
            Vector2 contactNormal = collision.GetContact(0).normal;
            Vector2 facingDir = GetFacingDirection();
            if (Vector2.Dot(contactNormal, -facingDir) < contactNormalThreshold)
                return;
        }

        var playerController = collision.collider.GetComponentInParent<IPlayerController>();
        if (playerController == null)
            return;

        int playerKey = ((MonoBehaviour)playerController).GetInstanceID();
        if (_lastBounceTime.TryGetValue(playerKey, out float lastTime) &&
            Time.time - lastTime < bounceCooldown)
            return;

        bool inBoostZone = false;
        if (boostZone != null)
            inBoostZone = boostZone.IsPlayerInside(collision.collider);
        else if (!requireBoostZoneForBonus)
            inBoostZone = true;

        Vector2 bounceVelocity = ComputeBounceVelocity(playerController.Velocity, inBoostZone);

        playerController.AddFrameForce(bounceVelocity, resetVelocity: true);

        _lastBounceTime[playerKey] = Time.time;
    }

    // ── Bounce math ─────────────────────────────────────────────────────

    private Vector2 ComputeBounceVelocity(Vector2 playerVelocity, bool applyBonus)
    {
        Vector2 n = GetFacingDirection();

        float alongMag = Vector2.Dot(playerVelocity, n);
        Vector2 along = alongMag * n;
        Vector2 side = playerVelocity - along;

        Vector2 reflected = -along + side;

        if (Vector2.Dot(reflected, n) < 0f)
            reflected = -reflected;

        if (reflected.magnitude < minimumBounceSpeed)
            reflected = n * minimumBounceSpeed;

        Vector2 result = reflected * bounceMultiplier;

        if (applyBonus)
            result += n * bonusSpeed;

        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Vector2 GetFacingDirection()
    {
        return facingAxis switch
        {
            FacingAxis.Up => transform.up,
            FacingAxis.Right => transform.right,
            _ => transform.up
        };
    }

    private bool IsColorActive()
    {
        if (colorManager == null)
            return true;

        foreach (var cp in colorManager.colorPlatforms)
        {
            if (cp.platformGroup == null) continue;

            if (string.Equals(cp.colorName, colorName, System.StringComparison.OrdinalIgnoreCase))
                return colorManager.IsPlatformGroupCurrentlyActive(cp.platformGroup);
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Vector2 facing = GetFacingDirection();
        Vector3 origin = transform.position;

        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawRay(origin, facing * 1.5f);
        Vector2 perp = Vector2.Perpendicular(facing) * 0.2f;
        Vector3 tip = origin + (Vector3)(facing * 1.5f);
        Gizmos.DrawLine(tip, tip - (Vector3)(facing * 0.3f) + (Vector3)perp);
        Gizmos.DrawLine(tip, tip - (Vector3)(facing * 0.3f) - (Vector3)perp);

        if (solidCollider != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawCube(solidCollider.bounds.center, solidCollider.bounds.size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(solidCollider.bounds.center, solidCollider.bounds.size);
        }

        if (boostZone != null)
        {
            var zoneColl = boostZone.GetComponent<Collider2D>();
            if (zoneColl != null)
            {
                Gizmos.color = new Color(0.9f, 0.9f, 0.1f, 0.2f);
                Gizmos.DrawCube(zoneColl.bounds.center, zoneColl.bounds.size);
                Gizmos.color = new Color(0.9f, 0.9f, 0.1f, 0.5f);
                Gizmos.DrawWireCube(zoneColl.bounds.center, zoneColl.bounds.size);
            }
        }
    }
#endif
}

