using UnityEngine;

/// <summary>
/// Straw/Slime enemy - stationary, reacts to damage.
/// ADD EnemyBase component alongside this.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(Health))]
public class strawEnemy : MonoBehaviour
{
    public Animator animator;
    public Health health;

    private EnemyBase enemyBase;

    void Awake()
    {
        health = GetComponent<Health>();
        enemyBase = GetComponent<EnemyBase>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamage;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamage;
    }

    void HandleDamage()
    {
        if (animator != null)
            animator.SetTrigger("isDamaged");
    }
}
