using UnityEngine;

/// <summary>
/// 激光子弹组件（可选，用于控制激光行为）
/// </summary>
public class LaserProjectile : MonoBehaviour
{
    private Rigidbody2D _rb;

    public void Initialize(Vector2 direction, float speed, float lifetime)
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
            _rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, Mathf.Max(0.01f, lifetime));
    }
}