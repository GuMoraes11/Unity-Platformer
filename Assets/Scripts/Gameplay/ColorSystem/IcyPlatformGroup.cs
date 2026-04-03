using UnityEngine;

/// <summary>
/// Attach this to the PARENT object of your ice platforms.
/// It will automatically apply ice settings to all child Collider2D objects,
/// so every child platform behaves like an icy/slippery platform.
///
/// IMPORTANT:
/// Your current PlayerController already checks for colliders tagged "Blue"
/// to enable its slippery surface logic, so this script can hook into that
/// without you needing to edit the controller again.
/// </summary>
[ExecuteAlways]
public class IcyPlatformGroup : MonoBehaviour
{
    [Header("Child Search")]
    [SerializeField] private bool includeInactiveChildren = true;
    [SerializeField] private bool includeParentColliderToo = false;

    [Header("Ice Identification")]
    [Tooltip("Must match what your PlayerController checks for. In your current setup, that is 'Blue'.")]
    [SerializeField] private string slipperyTag = "Blue";

    [Header("Physics Material (Ice Feel)")]
    [Tooltip("Lower = more slippery. 0 is very slick.")]
    [Range(0f, 1f)]
    [SerializeField] private float friction = 0f;

    [Tooltip("Usually keep this at 0 for ice.")]
    [Range(0f, 1f)]
    [SerializeField] private float bounciness = 0f;

    [Tooltip("Use Minimum so friction stays low even if the player has another material.")]
    [SerializeField] private PhysicsMaterialCombine2D frictionCombine = PhysicsMaterialCombine2D.Minimum;

    [Tooltip("Usually Maximum or Average is fine for bounce.")]
    [SerializeField] private PhysicsMaterialCombine2D bounceCombine = PhysicsMaterialCombine2D.Maximum;
    
    [Header("Collider Safety")]
    [SerializeField] private bool forceCollidersToNotBeTriggers = true;

    [Header("Auto Apply")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool applyOnValidate = true;

    private PhysicsMaterial2D _runtimeIceMaterial;

    private void Awake()
    {
        if (applyOnAwake)
            ApplyIceToChildren();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!applyOnValidate) return;

        // Delay call slightly so inspector edits settle cleanly.
        UnityEditor.EditorApplication.delayCall += DelayedApply;
    }

    private void DelayedApply()
    {
        if (this == null) return;
        ApplyIceToChildren();
    }
#endif

    [ContextMenu("Apply Ice To Children")]
    public void ApplyIceToChildren()
    {
        var colliders = GetComponentsInChildren<Collider2D>(includeInactiveChildren);
        if (colliders == null || colliders.Length == 0) return;

        EnsureMaterialExists();

        foreach (var col in colliders)
        {
            if (col == null) continue;

            // Skip parent collider if desired
            if (!includeParentColliderToo && col.transform == transform)
                continue;

            if (forceCollidersToNotBeTriggers)
                col.isTrigger = false;

            // Assign slippery tag so your PlayerController detects it
            if (!string.IsNullOrWhiteSpace(slipperyTag))
            {
                TrySetTag(col.gameObject, slipperyTag);
            }

            // Assign low-friction ice material
            col.sharedMaterial = _runtimeIceMaterial;
        }
    }

    private void EnsureMaterialExists()
    {
        if (_runtimeIceMaterial == null)
        {
            _runtimeIceMaterial = new PhysicsMaterial2D("Runtime_IceMaterial");
        }

        _runtimeIceMaterial.friction = friction;
        _runtimeIceMaterial.bounciness = bounciness;
        _runtimeIceMaterial.frictionCombine = frictionCombine;
        _runtimeIceMaterial.bounceCombine = bounceCombine;
    }

    private void TrySetTag(GameObject target, string tagName)
    {
        if (target == null) return;

        try
        {
            target.tag = tagName;
        }
        catch
        {
            Debug.LogWarning(
                $"IcyPlatformGroup on '{name}' could not set tag '{tagName}'. " +
                $"Make sure that tag exists in Unity's Tag Manager.",
                this
            );
        }
    }
}