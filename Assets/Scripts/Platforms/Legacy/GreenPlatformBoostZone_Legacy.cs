using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LEGACY — preserved copy of GreenPlatformBoostZone.cs before the v2 rewrite.
/// Class renamed to GreenPlatformBoostZone_Legacy to avoid duplicate symbol errors.
/// Original path: Assets/Scripts/Platforms/GreenPlatformBoostZone.cs
///
/// Trigger zone helper for <see cref="GreenPlatform_Legacy"/>.
///
/// Place this on a child GameObject with a trigger Collider2D that defines the
/// "boost region" of a green launch platform. When the player overlaps this
/// trigger at the moment they hit the solid collider, the platform applies
/// bonus momentum.
///
/// Tracks overlapping players via OnTriggerEnter2D / OnTriggerExit2D so the
/// parent <see cref="GreenPlatform_Legacy"/> can query <see cref="IsPlayerInside"/>
/// at collision time without per-frame physics queries.
///
/// Tracks by root Transform rather than individual Collider2D to avoid
/// "inside" flickering when the player swaps between colliders (e.g.,
/// BoxCollider2D grounded ↔ CapsuleCollider2D airborne in PlayerController).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GreenPlatformBoostZone_Legacy : MonoBehaviour
{
    /// <summary>
    /// Set of player root Transforms currently overlapping this trigger.
    /// Keyed by root Transform for stable identity across collider swaps.
    /// Ref-counted: incremented on Enter, decremented on Exit, removed at zero.
    /// </summary>
    private readonly Dictionary<Transform, int> _overlappingPlayers = new();

    /// <summary>
    /// Returns true if the player that owns the given collider is currently
    /// inside the boost zone. Resolves to root Transform for stable identity.
    /// Called by <see cref="GreenPlatform_Legacy"/> at collision time.
    /// </summary>
    public bool IsPlayerInside(Collider2D playerCollider)
    {
        if (playerCollider == null) return false;
        var root = playerCollider.transform.root;
        return _overlappingPlayers.TryGetValue(root, out int count) && count > 0;
    }

    /// <summary>
    /// Returns true if any player is currently inside the boost zone.
    /// </summary>
    public bool HasAnyPlayer()
    {
        return _overlappingPlayers.Count > 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var root = other.transform.root;
        _overlappingPlayers.TryGetValue(root, out int count);
        _overlappingPlayers[root] = count + 1;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var root = other.transform.root;
        if (_overlappingPlayers.TryGetValue(root, out int count))
        {
            if (count <= 1)
                _overlappingPlayers.Remove(root);
            else
                _overlappingPlayers[root] = count - 1;
        }
    }

    private void OnDisable()
    {
        _overlappingPlayers.Clear();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        // Ensure the collider is a trigger on first add
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
#endif
}

