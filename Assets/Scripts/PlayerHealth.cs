using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float invincibilityTime = 1f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Health UI")]
    [SerializeField] private SpriteRenderer[] healthSprites;
    [SerializeField] private Color damagedColor = Color.black; 
    [SerializeField] private float damagedOpacity = 0.3f;



    private int currentHealth;
    private Rigidbody2D rb;
    private bool isInvincible = false;



    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    void Start()
    {

    }

    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        UpdateHealthUI();
        Debug.Log($"Player took {damage} damage! Health: {currentHealth}");

        // Knockback
        Vector2 knockbackDirection = (transform.position - (Vector3)sourcePosition).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        SpriteRenderer sr = spriteRenderer;
        float blinkDuration = 0.1f;
        float elapsed = 0f;

        while (elapsed < invincibilityTime)
        {
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkDuration);
            elapsed += blinkDuration;
        }

        if (sr) sr.enabled = true;
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("Player died!");
        StartCoroutine(RestartLevel());
    }

    private IEnumerator RestartLevel()
    {
        yield return new WaitForSeconds(0f); // Optional delay
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateHealthUI()
    {
        for (int i = 0; i < healthSprites.Length; i++)
        {
            if (i < currentHealth)
            {
                // Full health
                healthSprites[i].color = Color.white;
            }
            else
            {
                // Damaged
                var color = damagedColor;
                color.a = damagedOpacity;
                healthSprites[i].color = color;
            }
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}
