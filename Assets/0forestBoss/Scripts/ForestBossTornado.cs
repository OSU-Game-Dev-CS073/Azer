using UnityEngine;

/// <summary>
/// Forest Boss Skill3 — Tornado.
/// 从屏幕边缘生成，向中心移动。碰到玩家造成伤害并推开。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ForestBossTornado : MonoBehaviour
{
    public float damage = 20f;
    public float speed = 3f;
    public float lifetime = 6f;
    public Vector2 moveDirection = Vector2.left;
    public float pushForce = 15f;

    private Rigidbody2D _rb;
    private float _timer;
    private bool _hit;

    public void Initialize(Vector2 moveDir, float spd, float dmg, float life)
    {
        moveDirection = moveDir.normalized;
        speed = spd;
        damage = dmg;
        lifetime = life;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.linearDamping = 0f;
        _rb.freezeRotation = true;
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = moveDirection * speed;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifetime)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hit) return;
        if (!collision.collider.CompareTag("Player")) return;

        Player player = collision.collider.GetComponentInParent<Player>();
        if (player == null) return;

        _hit = true;

        Vector2 knockDir = moveDirection.x >= 0 ? Vector2.right : Vector2.left;
        ForestBossDamageHelper.ApplyHit(player, knockDir, damage);

        if (player.rb != null)
            player.rb.linearVelocity = new Vector2(moveDirection.x * pushForce, player.rb.linearVelocity.y + 3f);
    }
}
