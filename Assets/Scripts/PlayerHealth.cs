using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Per-Player Damage Response")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float invincibilityTime = 1f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Pop Visuals")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody2D rb;
    private bool isInvincible = false;
    private bool isFrozen = false;

    private SpriteRenderer[] allRenderers;
    private Collider2D[] allColliders;

    private Behaviour controllerBehaviour;
    private Behaviour inputBehaviour;

    private Vector3 defaultVisualScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        allColliders = GetComponentsInChildren<Collider2D>(true);

        controllerBehaviour = GetComponent("PlayerController") as Behaviour;
        inputBehaviour = GetComponent("PlayerInput") as Behaviour;

        if (visualRoot == null)
            visualRoot = spriteRenderer != null ? spriteRenderer.transform.parent : transform;

        defaultVisualScale = visualRoot.localScale;
    }

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (isFrozen) return;
        if (isInvincible) return;

        if (SharedHealthManager.Instance == null)
        {
            Debug.LogError("No SharedHealthManager found in scene.");
            return;
        }

        if (!SharedHealthManager.Instance.CanTakeDamage()) return;

        int healthAfterHit = SharedHealthManager.Instance.GetCurrentHealth() - damage;
        SharedHealthManager.Instance.DealSharedDamage(damage, this);

        if (healthAfterHit <= 0) return;

        Vector2 knockbackDirection = (transform.position - (Vector3)sourcePosition).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(InvincibilityCoroutine());
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        float blinkDuration = 0.1f;
        float elapsed = 0f;

        while (elapsed < invincibilityTime)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(blinkDuration);
            elapsed += blinkDuration;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvincible = false;
    }

    public void FreezeForDeathSequence()
    {
        StopAllCoroutines();
        isInvincible = false;
        isFrozen = true;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (controllerBehaviour != null) controllerBehaviour.enabled = false;
        if (inputBehaviour != null) inputBehaviour.enabled = false;

        foreach (var col in allColliders)
        {
            if (col != null) col.enabled = false;
        }
    }

    public void RestoreAfterRespawn()
    {
        isFrozen = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        foreach (var col in allColliders)
        {
            if (col != null) col.enabled = true;
        }

        if (controllerBehaviour != null) controllerBehaviour.enabled = true;
        if (inputBehaviour != null) inputBehaviour.enabled = true;

        SetVisualState(defaultVisualScale, 1f);
    }

    public void PrepareHiddenAtRespawn()
    {
        FreezeForDeathSequence();
        SetVisualState(Vector3.zero, 0f);
    }

    public IEnumerator PlayPopOut(float duration)
    {
        FreezeForDeathSequence();

        Vector3 startScale = defaultVisualScale;
        Vector3 expandedScale = defaultVisualScale * 1.18f;
        float expandDuration = duration * 0.45f;

        float t = 0f;
        while (t < expandDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / expandDuration);
            SetVisualState(Vector3.Lerp(startScale, expandedScale, lerp), 1f);
            yield return null;
        }

        t = 0f;
        float shrinkDuration = Mathf.Max(0.01f, duration - expandDuration);

        while (t < shrinkDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / shrinkDuration);
            float eased = 1f - Mathf.Pow(1f - lerp, 3f);
            SetVisualState(Vector3.Lerp(expandedScale, Vector3.zero, eased), 1f - eased);
            yield return null;
        }

        SetVisualState(Vector3.zero, 0f);
    }

    public IEnumerator PlayPopIn(float duration)
    {
        SetVisualState(Vector3.zero, 0f);

        Vector3 overshootScale = defaultVisualScale * 1.15f;
        float firstPhase = duration * 0.7f;

        float t = 0f;
        while (t < firstPhase)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / firstPhase);
            float eased = 1f - Mathf.Pow(1f - lerp, 3f);
            SetVisualState(Vector3.Lerp(Vector3.zero, overshootScale, eased), eased);
            yield return null;
        }

        t = 0f;
        float settleDuration = Mathf.Max(0.01f, duration - firstPhase);

        while (t < settleDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / settleDuration);
            SetVisualState(Vector3.Lerp(overshootScale, defaultVisualScale, lerp), 1f);
            yield return null;
        }

        SetVisualState(defaultVisualScale, 1f);
        RestoreAfterRespawn();
    }

    private void SetVisualState(Vector3 scale, float alpha)
    {
        if (visualRoot != null)
            visualRoot.localScale = scale;

        foreach (var sr in allRenderers)
        {
            if (sr == null) continue;

            sr.enabled = alpha > 0.001f;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    public int GetCurrentHealth()
    {
        return SharedHealthManager.Instance != null ? SharedHealthManager.Instance.GetCurrentHealth() : 0;
    }

    public int GetMaxHealth()
    {
        return SharedHealthManager.Instance != null ? SharedHealthManager.Instance.GetMaxHealth() : 0;
    }
}