using UnityEngine;

/// <summary>
/// Projectile collision handler. Updated to use EnemyController.
/// </summary>
public class Projectile : MonoBehaviour
{
    public int damage = 10;
    public float lifetime = 3f;
    public bool appliesSlow = false;
    public float slowDuration = 0f;

    private float timer;

    void Start()
    {
        timer = lifetime;

        // Ignore collision with player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider2D playerCol = player.GetComponent<Collider2D>();
            Collider2D myCol = GetComponent<Collider2D>();
            if (playerCol != null && myCol != null)
            {
                Physics2D.IgnoreCollision(myCol, playerCol);
            }
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Don't hit the player
        if (other.CompareTag("Player")) return;

        // Hit enemy
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") || other.CompareTag("Enemy"))
        {
            Health enemyHealth = other.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-damage);
            }

            // Apply slow if ice bolt
            if (appliesSlow)
            {
                EnemyController enemy = other.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    enemy.ApplySlow(slowDuration);
                }
                else
                {
                    // Legacy support for old EnemyBase
                    EnemyBase legacyEnemy = other.GetComponent<EnemyBase>();
                    if (legacyEnemy != null)
                        legacyEnemy.ApplySlow(slowDuration);
                }
            }

            Destroy(gameObject);
            return;
        }

        // Hit ground/wall
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            Destroy(gameObject);
        }
    }
}
