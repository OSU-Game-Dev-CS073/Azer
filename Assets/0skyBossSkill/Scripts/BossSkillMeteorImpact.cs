using System.Collections;
using UnityEngine;

/// <summary>
/// ��ʯ��Ĭ�� Move������ײ�� Ground(layer) �����ʱ���� Boom��Boom �������١�
/// ��������һ�����˺����� BossSkillDamageHelper -> Player.TakeDamage����
/// </summary>
public class BossSkillMeteorImpact : MonoBehaviour
{
    [Min(0f)] public float damage = 15f;
    [Tooltip("碰撞时忽略带有这些 Tag 的物体")]
    public string[] ignoreTags;
    public string boomAnimStateName = "boom";
    public float fallbackBoomDuration = 0.4f;

    int _groundLayer;
    bool _booming;

    void Awake()
    {
        _groundLayer = LayerMask.NameToLayer("Ground");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other);
    }

    void HandleImpact(Collider2D other)
    {
        if (_booming) return;
        if (other == null) return;

        // 忽略带指定 Tag 的物体
        if (ignoreTags != null && ignoreTags.Length > 0)
        {
            foreach (var t in ignoreTags)
            {
                if (other.CompareTag(t)) return;
            }
        }

        if (other.CompareTag("Player") && other.TryGetComponent<Player>(out var player))
        {
            Vector2 knockDir = transform.position.x < other.transform.position.x ? Vector2.right : Vector2.left;
            BossSkillDamageHelper.ApplyHit(player, knockDir, damage);
            StartCoroutine(CoBoomAndDestroy());
            return;
        }

        // ��أ�layer=Ground
        if (_groundLayer != -1 && other.gameObject.layer == _groundLayer)
        {
            StartCoroutine(CoBoomAndDestroy());
        }
    }

    IEnumerator CoBoomAndDestroy()
    {
        _booming = true;

        // ֹͣ�ƶ������������͸/��ײ
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float wait = fallbackBoomDuration;
        var animator = GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(boomAnimStateName))
        {
            animator.Play(boomAnimStateName, 0, 0f);
            yield return null;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) wait = info.length;
        }

        if (wait <= 0f) wait = 0.15f;
        yield return new WaitForSeconds(wait);
        Destroy(gameObject);
    }
}

