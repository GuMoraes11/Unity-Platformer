using UnityEngine;

/// <summary>
/// Minimal trigger relay for <see cref="GreenPlatform"/>.
///
/// Place on a child GameObject with a trigger Collider2D. Forwards
/// OnTriggerEnter2D / OnTriggerExit2D to the owning GreenPlatform
/// via <see cref="GreenPlatform.NotifyZoneEnter"/> and
/// <see cref="GreenPlatform.NotifyZoneExit"/>.
///
/// Contains NO gameplay logic. All decisions are made by GreenPlatform.
///
/// Player identity is resolved to root Transform (other.transform.root)
/// before forwarding, so the owner always receives a stable identity
/// regardless of which player collider triggered the event.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GreenPadZoneRelay : MonoBehaviour
{
    public enum ZoneType { Outer, Inner }

    [Tooltip("Which zone this relay represents.")]
    public ZoneType zoneType = ZoneType.Outer;

    [Tooltip("The GreenPlatform that owns this zone. Auto-found in parent if null.")]
    public GreenPlatform owner;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<GreenPlatform>();

        if (owner == null)
        {
            Debug.LogError(
                $"[GreenPadZoneRelay] '{name}': No GreenPlatform owner found in parent hierarchy. " +
                "This relay will do nothing.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null) return;
        if (!other.CompareTag("Player")) return;
        owner.NotifyZoneEnter(other.transform.root, zoneType);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (owner == null) return;
        if (!other.CompareTag("Player")) return;
        owner.NotifyZoneExit(other.transform.root, zoneType);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Auto-set trigger on first add
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // Auto-find owner
        if (owner == null)
            owner = GetComponentInParent<GreenPlatform>();
    }
#endif
}

