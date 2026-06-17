using UnityEngine;

/// <summary>
/// Skeleton enemy with improved AI:
/// - PATROL: walks between patrol points when player not detected
/// - CHASE: moves toward player when detected
/// - ATTACK: attacks when in range, with cooldown
/// - RETREAT: brief backstep after attacking
/// 
/// ADD EnemyBase component alongside this on the GameObject.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(Health))]
public class Skeleton_Enemy : MonoBehaviour
{
    public enum AIState { Patrol, Chase, Attack, Retreat, Idle }

    #region Public Variables
    [Header("Detection")]
    public Transform rayCast;
    public LayerMask raycastMask;
    public float rayCastLength = 8f;
    public float detectionRange = 10f; // Range to notice player

    [Header("Combat")]
    public float attackDistance = 1.5f;
    public float attackCooldown = 2f;
    public int attackDamage = 10;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolSpeed = 1.5f;
    public Transform[] patrolPoints; // Optional patrol points
    public float retreatDistance = 2f;
    public float retreatDuration = 0.5f;

    [Header("References")]
    public Health health;
    #endregion

    #region Private Variables
    private RaycastHit2D hit;
    private GameObject target;
    private Animator anim;
    private float distance;
    private bool inRange;
    private float attackTimer;
    private AIState currentState = AIState.Idle;
    private EnemyBase enemyBase;
    private int patrolIndex = 0;
    private float retreatTimer = 0f;
    private SpriteRenderer spriteRenderer;
    #endregion

    void Awake()
    {
        anim = GetComponent<Animator>();
        health = GetComponent<Health>();
        enemyBase = GetComponent<EnemyBase>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        attackTimer = 0f;
    }

    void Update()
    {
        if (enemyBase != null && enemyBase.IsDead)
        {
            anim.SetBool("canWalk", false);
            anim.SetBool("Attack", false);
            return;
        }

        // Tick attack cooldown
        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        // Check for player
        DetectPlayer();

        // State machine
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
            case AIState.Retreat:
                UpdateRetreat();
                break;
        }
    }

    // ============ DETECTION ============

    void DetectPlayer()
    {
        // Try to find player if not in range
        if (!inRange)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                if (dist <= detectionRange)
                {
                    target = player;
                    inRange = true;
                    if (currentState == AIState.Idle || currentState == AIState.Patrol)
                    {
                        currentState = AIState.Chase;
                    }
                }
            }
        }

        // Raycast for line of sight
        if (inRange && target != null)
        {
            Vector2 dir = (target.transform.position.x < transform.position.x) ? Vector2.left : Vector2.right;
            hit = Physics2D.Raycast(rayCast.position, dir, rayCastLength, raycastMask);

            distance = Vector2.Distance(transform.position, target.transform.position);

            // Lost player
            if (distance > detectionRange * 1.5f)
            {
                inRange = false;
                target = null;
                currentState = patrolPoints != null && patrolPoints.Length > 0
                    ? AIState.Patrol
                    : AIState.Idle;
            }

            Debug.DrawRay(rayCast.position, dir * rayCastLength,
                distance <= attackDistance ? Color.green : Color.red);
        }
    }

    // ============ STATES ============

    void UpdateIdle()
    {
        anim.SetBool("canWalk", false);
        anim.SetBool("Attack", false);

        // Start patrolling if we have patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentState = AIState.Patrol;
        }
    }

    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentState = AIState.Idle;
            return;
        }

        anim.SetBool("canWalk", true);
        anim.SetBool("Attack", false);

        Transform targetPoint = patrolPoints[patrolIndex];
        float speed = patrolSpeed * GetSpeedMult();

        // Face patrol direction
        FaceTarget(targetPoint.position);

        transform.position = Vector2.MoveTowards(transform.position,
            new Vector2(targetPoint.position.x, transform.position.y),
            speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.3f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }

        // Switch to chase if player detected
        if (inRange) currentState = AIState.Chase;
    }

    void UpdateChase()
    {
        if (target == null)
        {
            currentState = AIState.Idle;
            return;
        }

        anim.SetBool("canWalk", true);
        anim.SetBool("Attack", false);

        // Face player
        FaceTarget(target.transform.position);

        // Move toward player
        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("skelly_attack"))
        {
            Vector2 targetPos = new Vector2(target.transform.position.x, transform.position.y);
            float speed = moveSpeed * GetSpeedMult();
            transform.position = Vector2.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        }

        // Switch to attack if in range
        if (distance <= attackDistance && attackTimer <= 0)
        {
            currentState = AIState.Attack;
        }
    }

    void UpdateAttack()
    {
        anim.SetBool("canWalk", false);
        anim.SetBool("Attack", true);

        attackTimer = attackCooldown;

        // Actual damage is dealt via DealDamage() called from animation event
        // or after a short delay
        Invoke(nameof(DealDamageDelayed), 0.4f);

        // After attack, retreat briefly
        currentState = AIState.Retreat;
        retreatTimer = retreatDuration;
    }

    void UpdateRetreat()
    {
        anim.SetBool("Attack", false);
        anim.SetBool("canWalk", true);

        retreatTimer -= Time.deltaTime;

        if (target != null)
        {
            // Move away from player briefly
            float dir = transform.position.x > target.transform.position.x ? 1f : -1f;
            float speed = moveSpeed * 0.5f * GetSpeedMult();
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0, 0);

            FaceTarget(target.transform.position);
        }

        if (retreatTimer <= 0)
        {
            currentState = inRange ? AIState.Chase : AIState.Idle;
        }
    }

    // ============ COMBAT ============

    void DealDamageDelayed()
    {
        if (enemyBase != null && enemyBase.IsDead) return;
        if (target == null) return;

        if (Vector2.Distance(transform.position, target.transform.position) <= attackDistance * 1.5f)
        {
            Player player = target.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
                Debug.Log($"Skeleton dealt {attackDamage} damage to player!");
            }
        }
    }

    // Animation event compatibility
    public void TriggerCooling()
    {
        // Cooldown is now handled by attackTimer
    }

    public void DealDamage()
    {
        DealDamageDelayed();
    }

    // ============ HELPERS ============

    void FaceTarget(Vector3 targetPos)
    {
        if (targetPos.x < transform.position.x)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    float GetSpeedMult()
    {
        return enemyBase != null ? enemyBase.GetSpeedMultiplier() : 1f;
    }

    // ============ TRIGGER DETECTION ============

    void OnTriggerEnter2D(Collider2D trig)
    {
        if (trig.gameObject.CompareTag("Player"))
        {
            target = trig.gameObject;
            inRange = true;
            if (currentState == AIState.Idle || currentState == AIState.Patrol)
            {
                currentState = AIState.Chase;
            }
        }
    }

    void OnTriggerExit2D(Collider2D trig)
    {
        if (trig.gameObject.CompareTag("Player"))
        {
            // Don't immediately lose target, let detection range handle it
        }
    }
}
