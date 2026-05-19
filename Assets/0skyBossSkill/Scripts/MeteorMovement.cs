using UnityEngine;

/// <summary>
/// 陨石移动组件（若陨石预制体没有自带移动，可添加此脚本）
/// </summary>
public class MeteorMovement : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 8f;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
            _rb.linearVelocity = Vector2.down * fallSpeed;
    }

    private void Update()
    {
        if (_rb == null)
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
    }
}