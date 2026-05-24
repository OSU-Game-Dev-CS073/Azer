using UnityEngine;

/// <summary>
/// Boss2 地刺伤害组件：对进入触发区域的玩家造成伤害。
/// 适用于技能2（锁定地刺）、技能3（全屏地刺）、技能4（虚空内刺）。
/// 默认命中一次后禁用碰撞，防止重复伤害。
/// </summary>
public class SpikeDamage : MonoBehaviour
{
    [Min(0f)] public float damage = 15f;
    [Tooltip("是否只命中一次（默认 true）")]
    public bool hitOnce = true;
    [Tooltip("命中冷却时间（秒），仅 hitOnce=false 时有效")]
    public float hitCooldown = 0.3f;

    private bool _hasHit;
    private float _cooldownTimer;

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!hitOnce && _cooldownTimer <= 0f)
            TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player")) return;
        if (_hasHit && hitOnce) return;
        if (_cooldownTimer > 0f) return;
        if (!other.TryGetComponent<Player>(out var player)) return;

        Vector2 knockDir = transform.position.x < other.transform.position.x ? Vector2.right : Vector2.left;
        BossSkillDamageHelper.ApplyHit(player, knockDir, damage);

        _cooldownTimer = hitCooldown;

        if (hitOnce)
        {
            _hasHit = true;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }
}
