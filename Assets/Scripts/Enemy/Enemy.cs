using UnityEngine;

/// <summary>
/// Bat enemy - patrols between points. 
/// Updated to use EnemyBase for health bar, death tracking, slow support.
/// ADD EnemyBase component alongside this on the GameObject.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public Transform[] points;

    [Header("References")]
    public Health health;
    public Animator animator;

    private int i;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private EnemyBase enemyBase;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        enemyBase = GetComponent<EnemyBase>();

        rb.gravityScale = 0f;

        if (points.Length > 0)
            transform.position = points[0].position;
    }

    void Update()
    {
        if (enemyBase != null && enemyBase.IsDead) return;

        if (points.Length == 0) return;

        if (Vector2.Distance(transform.position, points[i].position) < 0.01f)
        {
            i++;
            if (i == points.Length) i = 0;
        }

        // Apply speed with slow multiplier
        float currentSpeed = speed;
        if (enemyBase != null)
            currentSpeed *= enemyBase.GetSpeedMultiplier();

        transform.position = Vector2.MoveTowards(transform.position, points[i].position, currentSpeed * Time.deltaTime);
        spriteRenderer.flipX = (points[i].position.x - transform.position.x) < 0f;

        SetAnimation();
    }

    void SetAnimation()
    {
        if (animator == null) return;

        if (enemyBase != null && enemyBase.IsDead)
        {
            animator.Play("bat_death");
        }
        else
        {
            animator.Play("bat_fly");
        }
    }
}
