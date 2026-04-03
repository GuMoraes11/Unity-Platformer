
using UnityEngine;

public class FloatingTextEffect : MonoBehaviour
{
    [Header("Wobble Settings")]
    public float wobbleAmount = 1f;          // Degrees of tilt (small!)
    public float wobbleSpeed = 2f;           // How fast it wobbles

    [Header("Vertical Bob")]
    public float bobAmount = 0.05f;          // Slight up/down bob
    public float bobSpeed = 1f;

    private Quaternion originalRotation;
    private Vector3 originalPosition;

    void Start()
    {
        originalRotation = transform.localRotation;
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        // Simple 2D wobble using Z rotation only
        float wobbleAngle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;
        transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, wobbleAngle);

        // Gentle up/down bobbing
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = originalPosition + new Vector3(0f, bobOffset, 0f);
    }
}
