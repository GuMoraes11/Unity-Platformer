using UnityEngine;
using TarodevController;

/// <summary>
/// Yellow platform: speeds up player horizontal movement.
/// Amount is configurable in inspector.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class YellowPlatform : MonoBehaviour, ISpeedModifier
{
    [Header("Activation")]
    [SerializeField] private bool isActive = true;

    [Header("Speed Settings")]
    [Tooltip("1 = normal speed, 1.5 = 50% faster, 2 = double speed")]
    [SerializeField, Min(0f)] private float speedMultiplier = 1.5f;

    [Header("Color Gating (optional)")]
    [SerializeField] private bool gateByActiveColor = false;

    [Tooltip("Assign the SAME object used in ColorManager")]
    [SerializeField] private GameObject platformGroupRoot;

    [SerializeField] private ColorManager colorManager;

    [Header("References")]
    [SerializeField] private Collider2D platformCollider;

    public bool OnGround => true;
    public bool InAir => false;

    public Vector2 Modifier
    {
        get
        {
            if (!IsEffectActive())
                return Vector2.zero;

            // Controller does: finalMultiplier = 1 + modifier
            // So if speedMultiplier = 1.5, modifier must be +0.5
            float modifierX = speedMultiplier - 1f;
            return new Vector2(modifierX, 0f);
        }
    }

    private void Awake()
    {
        if (platformCollider == null)
            platformCollider = GetComponent<Collider2D>();

        if (platformCollider != null && !platformCollider.isTrigger)
        {
            Debug.LogWarning($"[YellowPlatform] '{name}' needs a TRIGGER collider.", this);
        }

        if (colorManager == null)
            colorManager = FindFirstObjectByType<ColorManager>();

        if (platformGroupRoot == null)
            platformGroupRoot = gameObject;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        speedMultiplier = Mathf.Max(0f, speedMultiplier);
    }
#endif

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