using UnityEngine;

[DisallowMultipleComponent]
public class RadialToggleable : MonoBehaviour
{
    [Tooltip("Which world index this object belongs to (index in ColorManager.colorPlatforms).")]
    public int worldIndex;

    SpriteRenderer[] srs;
    Collider2D[] cols;
    Vector3 cachedCenter;

    void Awake()
    {
        srs = GetComponentsInChildren<SpriteRenderer>(true);
        cols = GetComponentsInChildren<Collider2D>(true);
        cachedCenter = GetComponent<Renderer>() ? GetComponent<Renderer>().bounds.center : transform.position;
    }

    public Vector3 WorldCenter => cachedCenter;

    // Apply visual + physics state as if this object is active/inactive
    public void ApplyState(bool active, float activeAlpha, float inactiveAlpha)
    {
        float targetAlpha = active ? activeAlpha : inactiveAlpha;

        if (srs != null)
        {
            for (int i = 0; i < srs.Length; i++)
            {
                var c = srs[i].color;
                c.a = targetAlpha;
                srs[i].color = c;
            }
        }
        if (cols != null)
        {
            for (int i = 0; i < cols.Length; i++)
                cols[i].enabled = active;
        }
    }
}