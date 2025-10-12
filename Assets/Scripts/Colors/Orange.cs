using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Orange : MonoBehaviour
{
    [Header("Blink Settings")]
    [SerializeField] private float blinkDuration = 0.5f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Disappear Settings")]
    [SerializeField] private float disappearDuration = 1f;

    private Collider2D platformCollider;
    private SpriteRenderer spriteRenderer;
    private bool isActive = true;
    private ColorManager colorManager;

    void Awake()
    {
        platformCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        colorManager = Object.FindFirstObjectByType<ColorManager>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive || !collision.collider.CompareTag("Player")) return;
        StartCoroutine(DisappearRoutine());
    }

    private IEnumerator DisappearRoutine()
    {
        isActive = false;

        float elapsed = 0f;
        while (elapsed < blinkDuration)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(blinkInterval / 2f);

            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(blinkInterval / 2f);

            elapsed += blinkInterval;
        }

        spriteRenderer.enabled = false;
        platformCollider.enabled = false;

        yield return new WaitForSeconds(disappearDuration);

        // Determine visibility and interactivity based on current color
        bool orangeIsActive = IsOrangeCurrentlyActive();

        Color color = spriteRenderer.color;
        color.a = orangeIsActive ? colorManager.activeAlpha : colorManager.inactiveAlpha;
        spriteRenderer.color = color;

        spriteRenderer.enabled = true;
        platformCollider.enabled = orangeIsActive;

        isActive = true;
    }

    private bool IsOrangeCurrentlyActive()
    {
        if (colorManager == null) return false;

        foreach (var platform in colorManager.colorPlatforms)
        {
            if (platform.colorName.Equals("Orange", System.StringComparison.OrdinalIgnoreCase))
            {
                return colorManager.IsPlatformGroupCurrentlyActive(platform.platformGroup);
            }
        }

        return false;
    }


    private void Reset()
    {
        gameObject.tag = "Orange";
    }
}
