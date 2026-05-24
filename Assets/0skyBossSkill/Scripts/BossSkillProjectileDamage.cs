using System.Collections;
using UnityEngine;

/// <summary>
/// 通用投射物/技能碰撞伤害（BossSkill 专用），不改 Player 脚本。
/// 依赖 BossSkillDamageHelper.ApplyHit -> Player.TakeDamage。
/// </summary>
public class BossSkillProjectileDamage : MonoBehaviour
{
    public enum KnockbackMode
    {
        HorizontalRelativeToImpact,
        HorizontalFromVelocity,
        HorizontalFixed
    }

    [Header("Damage")]
    [Min(0f)] public float damage = 10f;

    [Header("On Hit (Animator)")]
    [Tooltip("命中玩家后播放的动画状态名（子弹 Animator 内应有 Move/Hit 两个状态）")]
    public string hitAnimStateName = "Hit";
    [Tooltip("命中后如果找不到 Animator/状态，延迟销毁（秒）")]
    public float fallbackDestroyDelay = 0f;
    public KnockbackMode knockbackMode = KnockbackMode.HorizontalRelativeToImpact;
    public Vector2 fixedKnockDir = Vector2.right;

    bool _hasHit;

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.collider);
    }

    void TryHit(Collider2D other)
    {
        if (_hasHit) return;
        if (other == null || !other.CompareTag("Player")) return;
        if (!other.TryGetComponent<Player>(out var player)) return;

        Vector2 knockDir = Vector2.right;
        switch (knockbackMode)
        {
            case KnockbackMode.HorizontalFromVelocity:
            {
                var projRb = GetComponent<Rigidbody2D>();
                if (projRb != null && projRb.linearVelocity.sqrMagnitude > 0.0001f)
                    knockDir = projRb.linearVelocity.x >= 0 ? Vector2.right : Vector2.left;
                else
                    knockDir = transform.position.x < other.transform.position.x ? Vector2.right : Vector2.left;
                break;
            }
            case KnockbackMode.HorizontalFixed:
                knockDir = fixedKnockDir;
                break;
            default:
                knockDir = transform.position.x < other.transform.position.x ? Vector2.right : Vector2.left;
                break;
        }

        BossSkillDamageHelper.ApplyHit(player, knockDir, damage);
        _hasHit = true;

        // 停止继续运动/重复碰撞
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 播放 Hit 动画，播完再销毁
        var animator = GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(hitAnimStateName))
        {
            StartCoroutine(CoPlayHitAndDestroy(animator));
            return;
        }

        if (fallbackDestroyDelay > 0f)
            Destroy(gameObject, fallbackDestroyDelay);
        else
            Destroy(gameObject);
    }

    IEnumerator CoPlayHitAndDestroy(Animator animator)
    {
        // 先切到 Hit（如果状态名不对，也不阻塞销毁）
        animator.Play(hitAnimStateName, 0, 0f);

        // 让 Animator 至少更新一帧以获取正确的 state info
        yield return null;

        float wait = 0f;
        try
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) wait = info.length;
        }
        catch { }

        // 兜底，避免 wait=0 导致“看不到 hit”
        if (wait <= 0f) wait = 0.15f;

        yield return new WaitForSeconds(wait);
        Destroy(gameObject);
    }
}

