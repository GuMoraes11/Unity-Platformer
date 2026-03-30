using UnityEngine.Rendering.Universal;
using UnityEngine;

public class  Yellow : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private float glowIncreaseSpeed = 1f;
    [SerializeField] private float glowDecreaseSpeed = 1f;
    [SerializeField] private float maxGlowIntensity = 5f;

    private bool playerOnPlatform = false;
    private Light2D playerLight;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerLight == null)
            {
                playerLight = other.GetComponentInChildren<Light2D>();
            }
            playerOnPlatform = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnPlatform = false;
        }
    }

    void Update()
    {
        if (playerLight == null) return;

        float speed = playerOnPlatform ? glowIncreaseSpeed : -glowDecreaseSpeed;
        float newIntensity = Mathf.Clamp(playerLight.intensity + speed * Time.deltaTime, 0f, maxGlowIntensity);
        playerLight.intensity = newIntensity;
    }
}