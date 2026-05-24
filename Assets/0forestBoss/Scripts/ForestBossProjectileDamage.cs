using System.Collections;
using UnityEngine;

/// <summary>
/// 森林Boss投射物碰撞伤害。命中玩家后播放Hit动画并销毁。
/// </summary>
public class ForestBossProjectileDamage : MonoBehaviour
{
    public enum KnockbackMode
    {
        HorizontalRelativeToImpact,
        HorizontalFromVelocity,
        HorizontalFixed
    }

    [Header("Damage")]
    [Min(0f)] public float damage = 10f;

    [Header("On Hit")]
    [Tooltip("命中后播放的动画状态名")]
    public string hitAnimStateName = "Hit";
    [Tooltip("命中后延迟销毁（秒），不播动画时有效")]
    public float fallbackDestroyDelay = 0f;
    public KnockbackMode knockbackMode = KnockbackMode.HorizontalRelativeToImpact;
    public Vector2 fixedKnockDir = Vector2.right;

    private bool _hasHit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.collider);
    }

    private void TryHit(Collider2D other)
    {
        if (_hasHit) return;
        if (other == null || !other.CompareTag("Player")) return;
        if (!other.TryGetComponent<Player>(out var player)) return;

        Debug.Log($"[ForestBossBullet] 命中玩家！damage={damage}, obj={gameObject.name}");

        Vector2 knockDir;
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

        ForestBossDamageHelper.ApplyHit(player, knockDir, damage);
        _hasHit = true;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 只有 Animator 真的有 Hit 状态时才播动画，否则立刻删
        var animator = GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(hitAnimStateName)
            && animator.HasState(0, Animator.StringToHash(hitAnimStateName)))
        {
            StartCoroutine(CoPlayHitAndDestroy(animator));
            return;
        }

        Debug.Log($"[ForestBossBullet] 立即销毁: {gameObject.name}");
        Destroy(gameObject);
    }

    private IEnumerator CoPlayHitAndDestroy(Animator animator)
    {
        animator.Play(hitAnimStateName, 0, 0f);
        yield return null;

        float wait = 0f;
        try
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) wait = info.length;
        }
        catch { }

        if (wait <= 0f) wait = 0.15f;
        yield return new WaitForSeconds(wait);
        Destroy(gameObject);
    }
}
