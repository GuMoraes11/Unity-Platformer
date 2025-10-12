using System.Collections.Generic;
using UnityEngine;

public class RadialWorldSwapController : MonoBehaviour
{
    [Header("Refs")]
    public ColorManager colorManager;
    public Transform player;

    [Header("Wave")]
    public float duration = 0.5f;
    public float maxRadius = 50f; // tune to cover your biggest level room
    public AnimationCurve radiusCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Input")]
    public KeyCode swapLeft = KeyCode.Q;
    public KeyCode swapRight = KeyCode.E;

    List<RadialToggleable> toggles = new List<RadialToggleable>();

    bool swapping = false;
    Vector3 center;
    float t; // 0..1
    int startIndex, targetIndex;
    float activeAlpha, inactiveAlpha;

    void Awake()
    {
        toggles.AddRange(FindObjectsOfType<RadialToggleable>(true));
        activeAlpha = colorManager ? colorManager.activeAlpha : 1f;
        inactiveAlpha = colorManager ? colorManager.inactiveAlpha : 0.25f;
    }

    void Update()
    {
        if (!swapping)
        {
            if (Input.GetKeyDown(swapLeft))  BeginSwap(-1);
            if (Input.GetKeyDown(swapRight)) BeginSwap(+1);
        }
        else
        {
            t += Time.deltaTime / duration;
            float radius = maxRadius * radiusCurve.Evaluate(Mathf.Clamp01(t));
            ApplyWave(radius);

            if (t >= 1f)
            {
                // Finalize: set color manager to the target world globally
                colorManager.SetIndexWithoutApplying(targetIndex);
                colorManager.ApplyImmediate();
                swapping = false;
            }
        }
    }

    void BeginSwap(int direction)
    {
        if (!colorManager || toggles.Count == 0 || !player) return;

        swapping = true;
        t = 0f;
        center = player.position;
        startIndex = colorManager.CurrentIndex;
        int count = colorManager.colorPlatforms.Count;
        targetIndex = (startIndex + (direction < 0 ? -1 : 1) + count) % count;

        // On start, make sure everything currently looks like the startIndex
        // (so we have a consistent baseline to blend from).
        ApplyAllAsIndex(startIndex);
    }

    void ApplyAllAsIndex(int index)
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            bool active = (toggles[i].worldIndex == index);
            toggles[i].ApplyState(active, activeAlpha, inactiveAlpha);
        }
        // Gray world remains always active: ColorManager already handles that on finalize. 
        // If you want gray visible/solid during the wave too, leave it as-is (do nothing here).
    }

    void ApplyWave(float radius)
    {
        float r2 = radius * radius;

        for (int i = 0; i < toggles.Count; i++)
        {
            var tg = toggles[i];
            // Inside the circle -> use target index; outside -> use start index
            bool inside = (tg.WorldCenter - center).sqrMagnitude <= r2;
            int idx = inside ? targetIndex : startIndex;
            bool active = (tg.worldIndex == idx);
            tg.ApplyState(active, activeAlpha, inactiveAlpha);
        }
    }
}
