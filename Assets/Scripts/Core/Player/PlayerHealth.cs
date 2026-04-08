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

    [Header("Death Burst")]
    [SerializeField] private DeathBurstEffect deathBurstPrefab;
    [SerializeField] private Color neutralBurstColor = Color.gray;
    [SerializeField] private Color activeBurstColor = Color.white;
    [SerializeField] private bool useSpriteColorIfAvailable = true;

    private Rigidbody2D rb;
    private bool isInvincible = false;
    private bool isFrozen = false;
    private bool externalInvulnerable = false;

    private SpriteRenderer[] allRenderers;
    private Collider2D[] allColliders;

    private Behaviour controllerBehaviour;
    private Behaviour inputBehaviour;

    private Vector3 defaultVisualScale;
    private Vector2 lastVelocity;
    private Color currentBurstColor;

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
        currentBurstColor = activeBurstColor;
    }

    private void Update()
    {
        if (rb != null && rb.simulated)
            lastVelocity = rb.linearVelocity;
    }

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (isFrozen) return;
        if (isInvincible) return;
        if (externalInvulnerable) return;

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
            rb.simulated = false;
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
            rb.simulated = true;
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

    public IEnumerator PlayDeathBurst(bool isPrimary, float duration)
    {
        Vector2 sampledVelocity = lastVelocity;
        FreezeForDeathSequence();

        Color burstColor = ResolveBurstColor();

        Vector2 direction;
        float intensity;
        float directionalBias;

        if (sampledVelocity.sqrMagnitude < 0.04f)
        {
            direction = Vector2.up;
            intensity = isPrimary ? 1.0f : 0.8f;
            directionalBias = isPrimary ? 0.35f : 0.15f;
        }
        else
        {
            direction = isPrimary
                ? -sampledVelocity.normalized
                : sampledVelocity.normalized;

            intensity = isPrimary ? 1.25f : 0.9f;
            directionalBias = isPrimary ? 0.9f : 0.45f;
        }

        if (deathBurstPrefab != null)
        {
            DeathBurstEffect burst = Instantiate(deathBurstPrefab, transform.position, Quaternion.identity);
            burst.PlayBurst(burstColor, direction, intensity, directionalBias);
        }

        Vector3 startScale = defaultVisualScale;
        Vector3 squashScale = isPrimary
            ? new Vector3(defaultVisualScale.x * 1.15f, defaultVisualScale.y * 0.85f, 1f)
            : new Vector3(defaultVisualScale.x * 1.08f, defaultVisualScale.y * 0.92f, 1f);

        float squashDuration = duration * 0.25f;
        float vanishDuration = Mathf.Max(0.01f, duration - squashDuration);

        float t = 0f;
        while (t < squashDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / squashDuration);
            SetVisualState(Vector3.Lerp(startScale, squashScale, lerp), 1f);
            yield return null;
        }

        t = 0f;
        while (t < vanishDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / vanishDuration);
            float eased = 1f - Mathf.Pow(1f - lerp, 3f);
            SetVisualState(Vector3.Lerp(squashScale, Vector3.zero, eased), 1f - eased);
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

    public void SetBurstColor(Color color)
    {
        currentBurstColor = color;
    }

    public void SetNeutralBurst()
    {
        currentBurstColor = neutralBurstColor;
    }

    public void SetExternalInvulnerable(bool value)
    {
        externalInvulnerable = value;
    }

    public bool IsExternallyInvulnerable()
    {
        return externalInvulnerable;
    }

    private Color ResolveBurstColor()
    {
        if (useSpriteColorIfAvailable && spriteRenderer != null)
            return spriteRenderer.color;

        return currentBurstColor;
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