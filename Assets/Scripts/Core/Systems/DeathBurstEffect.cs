using UnityEngine;

public class DeathBurstEffect : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem blobParticles;
    [SerializeField] private ParticleSystem streakParticles;
    [SerializeField] private ParticleSystem glowParticles;

    [Header("Tuning")]
    [SerializeField] private float baseBlobSpeedMultiplier = 1f;
    [SerializeField] private float baseStreakSpeedMultiplier = 1f;
    [SerializeField] private float destroyDelay = 2f;

    public void PlayBurst(Color color, Vector2 burstDirection, float intensity, float directionalBias)
    {
        burstDirection = burstDirection.sqrMagnitude > 0.001f ? burstDirection.normalized : Vector2.up;

        ConfigureSystem(blobParticles, color, burstDirection, intensity, directionalBias, 26f, baseBlobSpeedMultiplier);
        ConfigureSystem(streakParticles, color, burstDirection, intensity, directionalBias, 12f, baseStreakSpeedMultiplier);
        ConfigureGlow(glowParticles, color, intensity);

        if (blobParticles != null) blobParticles.Play();
        if (streakParticles != null) streakParticles.Play();
        if (glowParticles != null) glowParticles.Play();

        Destroy(gameObject, destroyDelay);
    }

    private void ConfigureSystem(
        ParticleSystem ps,
        Color color,
        Vector2 direction,
        float intensity,
        float directionalBias,
        float coneAngle,
        float speedMultiplier)
    {
        if (ps == null) return;

        Transform t = ps.transform;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        t.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        var main = ps.main;
        var shape = ps.shape;

        main.startColor = color;
        main.startSpeedMultiplier = Mathf.Max(0.1f, intensity * speedMultiplier);

        // Higher directionalBias = tighter cone, lower = more radial
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = Mathf.Lerp(360f, coneAngle, Mathf.Clamp01(directionalBias));
    }

    private void ConfigureGlow(ParticleSystem ps, Color color, float intensity)
    {
        if (ps == null) return;

        var main = ps.main;
        Color glowColor = color;
        glowColor.a = Mathf.Clamp01(glowColor.a);
        main.startColor = glowColor;
        main.startSpeedMultiplier = Mathf.Max(0.1f, intensity * 0.5f);
    }
}