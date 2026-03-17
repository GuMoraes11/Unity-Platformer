using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Per-Player Damage Response")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float invincibilityTime = 1f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private bool isInvincible = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (isInvincible) return;
        if (SharedHealthManager.Instance == null)
        {
            Debug.LogError("No SharedHealthManager found in scene.");
            return;
        }

        if (!SharedHealthManager.Instance.CanTakeDamage()) return;

        SharedHealthManager.Instance.DealSharedDamage(damage);

        // Knockback still applies to the player who got hit
        Vector2 knockbackDirection = (transform.position - (Vector3)sourcePosition).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        // Only start invincibility blink if the team is still alive
        if (SharedHealthManager.Instance.GetCurrentHealth() > 0)
        {
            StartCoroutine(InvincibilityCoroutine());
        }
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

    public int GetCurrentHealth()
    {
        return SharedHealthManager.Instance != null ? SharedHealthManager.Instance.GetCurrentHealth() : 0;
    }

    public int GetMaxHealth()
    {
        return SharedHealthManager.Instance != null ? SharedHealthManager.Instance.GetMaxHealth() : 0;
    }
}