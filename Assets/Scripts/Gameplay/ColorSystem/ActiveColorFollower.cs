using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ActiveColorFollower : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private bool preserveAlpha = true;

    private void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (ColorManager.Instance != null)
        {
            ColorManager.Instance.OnSharedActiveColorChanged += ApplyColor;
            ApplyColor(ColorManager.Instance.CurrentSharedColor);
        }
    }

    private void OnDisable()
    {
        if (ColorManager.Instance != null)
            ColorManager.Instance.OnSharedActiveColorChanged -= ApplyColor;
    }

    private void ApplyColor(Color newColor)
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (targetRenderer == null)
            return;

        if (preserveAlpha)
            newColor.a = targetRenderer.color.a;

        targetRenderer.color = newColor;
    }
}