using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SharedHealthManager : MonoBehaviour
{
    public static SharedHealthManager Instance { get; private set; }

    [Header("Shared Health Settings")]
    [SerializeField] private int maxHealth = 3;

    [Header("Shared Health UI")]
    [SerializeField] private RawImage[] healthImages;
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color damagedColor = Color.black;
    [SerializeField] private float damagedOpacity = 0.3f;

    private int currentHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public bool CanTakeDamage()
    {
        return currentHealth > 0;
    }

    public void DealSharedDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateHealthUI()
    {
        if (healthImages == null) return;

        for (int i = 0; i < healthImages.Length; i++)
        {
            if (healthImages[i] == null) continue;

            if (i < currentHealth)
            {
                healthImages[i].color = fullColor;
                healthImages[i].enabled = true;
            }
            else
            {
                Color c = damagedColor;
                c.a = damagedOpacity;
                healthImages[i].color = c;
                healthImages[i].enabled = true;
            }
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}