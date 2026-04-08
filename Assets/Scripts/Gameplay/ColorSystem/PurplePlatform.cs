using UnityEngine;
using TarodevController;

/// <summary>
/// Purple platform: slows player horizontal movement.
/// Amount is configurable in inspector.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PurplePlatform : MonoBehaviour, ISpeedModifier
{
    [Header("Activation")]
    [SerializeField] private bool isActive = true;

    [Header("Slow Settings")]
    [Tooltip("1 = normal speed, 0.5 = half speed, 0 = no movement")]
    [SerializeField, Range(0f, 1f)] private float speedMultiplier = 0.5f;

    [Header("Color Gating (optional)")]
    [SerializeField] private bool gateByActiveColor = false;

    [Tooltip("Assign the SAME object used in ColorManager")]
    [SerializeField] private GameObject platformGroupRoot;

    [SerializeField] private ColorManager colorManager;

    [Header("References")]
    [SerializeField] private Collider2D platformCollider;

    // ── ISpeedModifier ─────────────────────────────
    public bool OnGround => true;
    public bool InAir => false;

    public Vector2 Modifier
    {
        get
        {
            if (!IsEffectActive())
                return Vector2.zero;

            // Convert multiplier → additive modifier
            float modifierX = -(1f - speedMultiplier);
            return new Vector2(modifierX, 0f);
        }
    }

    // ── Setup ──────────────────────────────────────
    private void Awake()
    {
        if (platformCollider == null)
            platformCollider = GetComponent<Collider2D>();

        if (platformCollider != null && !platformCollider.isTrigger)
        {
            Debug.LogWarning($"[PurplePlatform] '{name}' needs a TRIGGER collider.", this);
        }

        if (colorManager == null)
            colorManager = FindFirstObjectByType<ColorManager>();

        if (platformGroupRoot == null)
            platformGroupRoot = gameObject;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        speedMultiplier = Mathf.Clamp01(speedMultiplier);
    }
#endif

    // ── Logic ──────────────────────────────────────
    private bool IsEffectActive()
    {
        if (!isActive)
            return false;

        if (!gateByActiveColor)
            return true;

        if (colorManager == null || platformGroupRoot == null)
            return true;

        return colorManager.IsGroupActive(platformGroupRoot);
    }
}