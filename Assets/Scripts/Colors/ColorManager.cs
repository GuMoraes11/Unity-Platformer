using System.Collections.Generic;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    [System.Serializable]
    public class ColorPlatform
    {
        [Header("Info")]
        public string colorName;
        public GameObject platformGroup;

        [Header("Background")]
        public GameObject backgroundGroup;
    }

    public enum CycleDirection { Left = -1, Right = 1 }

    [Header("Always Active (Gray)")]
    public GameObject grayWorld;

    [Header("Color Platforms")]
    public List<ColorPlatform> colorPlatforms = new List<ColorPlatform>();

    [Header("Opacity Settings")]
    [Range(0f, 1f)] public float activeAlpha = 1f;
    [Range(0f, 1f)] public float inactiveAlpha = 0.25f;

    [Header("Input (set false when using RadialWorldSwapController)")]
    [SerializeField] private bool handleInput = false;   // NEW: let radial controller own inputs

    [SerializeField] private int currentIndex = 0;

    // NEW: expose read-only index for radial controller
    public int CurrentIndex => currentIndex;

    void Start()
    {
        UpdatePlatformStates();
    }

    void Update()
    {
        if (!handleInput) return;

        if (Input.GetKeyDown(KeyCode.Q))
            CycleColor(CycleDirection.Left);
        else if (Input.GetKeyDown(KeyCode.E))
            CycleColor(CycleDirection.Right);
    }

    // NEW: allow external systems to move index WITHOUT applying immediately
    public void SetIndexWithoutApplying(int newIndex)
    {
        if (colorPlatforms.Count == 0) return;
        currentIndex = (newIndex % colorPlatforms.Count + colorPlatforms.Count) % colorPlatforms.Count;
    }

    // NEW: allow external systems to trigger a global apply (finalize after radial)
    public void ApplyImmediate()
    {
        UpdatePlatformStates();
    }

    // (optionally keep this public for debug buttons / menus)
    public void CycleColor(CycleDirection direction)
    {
        int step = (int)direction;
        currentIndex = (currentIndex + step + colorPlatforms.Count) % colorPlatforms.Count;
        UpdatePlatformStates();
    }

    // Make public so tools/UI can force refresh
    public void UpdatePlatformStates()
    {
        // Always enable gray
        SetPlatformActive(grayWorld, true, activeAlpha);

        for (int i = 0; i < colorPlatforms.Count; i++)
        {
            bool isActive = (i == currentIndex);
            float alpha = isActive ? activeAlpha : inactiveAlpha;

            SetPlatformActive(colorPlatforms[i].platformGroup, isActive, alpha);

            // Toggle background group fully on/off
            if (colorPlatforms[i].backgroundGroup != null)
            {
                colorPlatforms[i].backgroundGroup.SetActive(isActive);
            }
        }
    }

    void SetPlatformActive(GameObject group, bool active, float alpha)
    {
        if (group == null) return;

        foreach (var sr in group.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = sr.color;
            color.a = alpha;
            sr.color = color;
        }

        foreach (var col in group.GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = active;
        }
    }

    public float GetAlphaForTag(string tag)
    {
        for (int i = 0; i < colorPlatforms.Count; i++)
        {
            var platform = colorPlatforms[i];
            if (platform.platformGroup != null)
            {
                if (platform.platformGroup.CompareTag(tag))
                    return i == currentIndex ? activeAlpha : inactiveAlpha;
            }
        }

        return inactiveAlpha;
    }

    public bool IsPlatformGroupCurrentlyActive(GameObject group)
    {
        if (group == null) return false;
        return colorPlatforms.Count > 0 && colorPlatforms[currentIndex].platformGroup == group;
    }
}
