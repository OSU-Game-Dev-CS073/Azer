using UnityEngine;

/// <summary>
/// Simple visual effect: pulsing scale and fading trail.
/// </summary>
public class ProjectileVFX : MonoBehaviour
{
    private float pulseSpeed = 8f;
    private float pulseAmount = 0.15f;
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * pulse;
    }
}
