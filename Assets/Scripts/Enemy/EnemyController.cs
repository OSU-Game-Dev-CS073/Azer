using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// ╔══════════════════════════════════════════════════════════════════╗
/// ║                    ENEMY CONTROLLER v2.0                        ║
/// ║                                                                  ║
/// ║  Unified enemy system. ONE component does everything.            ║
/// ║  Replaces: Enemy.cs, EnemyBase.cs, skeletonEnemy.cs,            ║
/// ║            strawEnemy.cs                                         ║
/// ║                                                                  ║
/// ║  SETUP:                                                          ║
/// ║  1. Create empty GameObject, name it (e.g. "Skeleton_01")       ║
/// ║  2. Add SpriteRenderer → assign enemy sprite                     ║
/// ║  3. Add BoxCollider2D or CapsuleCollider2D                      ║
/// ║  4. Add Rigidbody2D (Freeze Rotation Z)                        ║
/// ║  5. Add Animator (if using animations)                          ║
/// ║  6. Add THIS component (EnemyController)                       ║
/// ║     → Health component auto-added                                ║
/// ║  7. Set layer to "Enemy", tag to "Enemy"                        ║
/// ║  8. Configure all fields in Inspector                           ║
/// ║  9. For patrol: create empty child GameObjects as waypoints     ║
/// ╚══════════════════════════════════════════════════════════════════╝
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    // ════════════════════════════════════════════
    //  ENUMS
    // ════════════════════════════════════════════

    public enum EnemyType { Mob, Boss }

    public enum AIBehavior
    {
        [Tooltip("Stays in place. Attacks if player enters range.")]
        Stationary,
        [Tooltip("Walks between patrol points on the ground. Chases player if detected.")]
        Patrol,
        [Tooltip("Flies between patrol points (no gravity). Does not chase.")]
        FlyingPatrol,
        [Tooltip("Flies between patrol points (no gravity). Chases and attacks the player.")]
        FlyingAggressive
    }

    private enum AIState { Idle, Patrol, Chase, Attack, Retreat, Dead }

    // ════════════════════════════════════════════
    //  INSPECTOR FIELDS
    // ════════════════════════════════════════════

    [Header("══ IDENTITY ══")]
    [Tooltip("Display name shown above the enemy.")]
    public string enemyName = "Enemy";

    [Tooltip("Mob = normal enemy. Boss = larger health bar, name always visible.")]
    public EnemyType enemyType = EnemyType.Mob;

    [Tooltip("XP given to player on death. Mobs: 15-50, Bosses: 100-500.")]
    public int xpReward = 25;

    [Tooltip("Gold dropped on death. 0 = no gold drop.")]
    public int goldDrop = 0;

    [Header("══ HEALTH ══")]
    [Tooltip("Maximum hit points. Set this here — the Health component's maxHealth will be overridden.")]
    public int maxHealth = 50;

    [Tooltip("Offset of the health bar above the enemy. Adjust Y to raise/lower it.")]
    public Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Offset of the name label above the health bar.")]
    public float nameLabelHeight = 0.4f;

    [Header("══ AI BEHAVIOR ══")]
    [Tooltip("How this enemy behaves.\n\nStationary: stands still, attacks if player is in range.\nPatrol: walks between points, chases player.\nFlyingPatrol: flies between points, no gravity, passive.\nFlyingAggressive: flies between points, chases and attacks player.")]
    public AIBehavior aiBehavior = AIBehavior.Patrol;

    [Header("══ MOVEMENT ══")]
    [Tooltip("Speed when patrolling between points.")]
    public float patrolSpeed = 2f;

    [Tooltip("Speed when chasing the player. (Ignored for Stationary and FlyingPatrol.)")]
    public float chaseSpeed = 4f;

    [Tooltip("Patrol waypoints. Create empty child GameObjects and drag them here.\nFor FlyingPatrol: place them wherever you want.\nFor Patrol: place at ground level.")]
    public Transform[] patrolPoints;

    [Tooltip("If true, waits briefly at each patrol point before continuing.")]
    public bool waitAtPoints = false;

    [Tooltip("Seconds to wait at each patrol point (if Wait At Points is enabled).")]
    public float waitDuration = 1f;

    [Header("══ COMBAT ══")]
    [Tooltip("If false, this enemy never attacks (decorative / obstacle only).")]
    public bool attacksPlayer = true;

    [Tooltip("Damage dealt per attack.")]
    public int attackDamage = 10;

    [Tooltip("Distance at which this enemy can hit the player.")]
    public float attackRange = 1.5f;

    [Tooltip("Distance at which this enemy detects and starts chasing the player.")]
    public float detectionRange = 8f;

    [Tooltip("Seconds between attacks.")]
    public float attackCooldown = 2f;

    [Tooltip("Deals damage on touch (like spikes). Separate from melee attacks.")]
    public bool contactDamage = false;

    [Tooltip("Damage dealt on contact (if Contact Damage is enabled).")]
    public int contactDamageAmount = 10;

    [Tooltip("Briefly step backward after attacking. Looks more natural.")]
    public bool retreatAfterAttack = true;

    [Tooltip("How far to step back after attacking.")]
    public float retreatDistance = 1.5f;

    [Tooltip("How long the retreat lasts (seconds).")]
    public float retreatDuration = 0.5f;

    [Header("══ ANIMATION ══")]
    [Tooltip("Leave blank if this enemy has no Animator.\nAnimation names must match your Animator clip names.")]
    public string idleAnim = "";
    public string walkAnim = "";
    public string attackAnim = "";
    public string hurtAnim = "";
    public string deathAnim = "";

    [Header("══ SFX ══")]
    [Tooltip("Sound played when attacking.")]
    public AudioClip attackSFX;
    [Tooltip("Sound played when taking damage.")]
    public AudioClip hurtSFX;
    [Tooltip("Sound played on death.")]
    public AudioClip deathSFX;
    [Range(0f, 1f)]
    public float sfxVolume = 0.6f;

    // ════════════════════════════════════════════
    //  RUNTIME STATE (not visible in Inspector)
    // ════════════════════════════════════════════

    private AIState currentState = AIState.Idle;
    private Health health;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private AudioSource audioSource;

    // Patrol
    private int patrolIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    // Combat
    private Transform playerTarget;
    private float attackTimer = 0f;
    private float retreatTimer = 0f;
    private float contactCooldownTimer = 0f;
    private bool isAttacking = false;

    // Slow effect
    private bool isSlowed = false;
    private float slowTimer = 0f;
    private Color originalColor = Color.white;

    // Death
    private bool isDead = false;

    // Health bar UI (created at runtime)
    private GameObject uiRoot;
    private Image healthBarFill;
    private Image healthBarTrail;
    private RectTransform fillRt;
    private RectTransform trailRt;
    private TextMeshProUGUI nameText;
    private bool healthBarVisible = false;
    private float healthBarTarget = 1f;
    private float healthBarTrailValue = 1f;

    // ════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════

    void Awake()
    {
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.7f;
        }
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
        }
    }

    void Start()
    {
        // Sync Health component with our maxHealth field
        if (health != null)
        {
            health.maxHealth = maxHealth;
            health.currentHealth = maxHealth;
        }

        // Gravity setup
        if (rb != null)
        {
            if (aiBehavior == AIBehavior.FlyingPatrol || aiBehavior == AIBehavior.FlyingAggressive)
                rb.gravityScale = 0f;

            rb.freezeRotation = true;
        }

        // Store original color
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Snap to first patrol point
        if (patrolPoints != null && patrolPoints.Length > 0 && aiBehavior != AIBehavior.Stationary)
            transform.position = patrolPoints[0].position;

        // Start in patrol or idle
        if (patrolPoints != null && patrolPoints.Length > 0 && aiBehavior != AIBehavior.Stationary)
            currentState = AIState.Patrol;
        else
            currentState = AIState.Idle;

        // Create UI
        CreateUI();
    }

    void Update()
    {
        // Always update UI position
        UpdateUI();

        if (isDead) return;
        if (isAttacking) return; // let attack animation play without interruption

        // Timers
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (contactCooldownTimer > 0) contactCooldownTimer -= Time.deltaTime;

        // Slow effect
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0) RemoveSlow();
        }

        // Player detection
        DetectPlayer();

        // State machine
        switch (currentState)
        {
            case AIState.Idle: UpdateIdle(); break;
            case AIState.Patrol: UpdatePatrol(); break;
            case AIState.Chase: UpdateChase(); break;
            case AIState.Attack: UpdateAttack(); break;
            case AIState.Retreat: UpdateRetreat(); break;
        }
    }

    // ════════════════════════════════════════════
    //  PLAYER DETECTION
    // ════════════════════════════════════════════

    void DetectPlayer()
    {
        if (!attacksPlayer) return;
        if (aiBehavior == AIBehavior.FlyingPatrol) return; // passive flying enemies don't chase

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                if (dist <= detectionRange)
                {
                    playerTarget = player.transform;
                    if (currentState == AIState.Idle || currentState == AIState.Patrol)
                        currentState = AIState.Chase;
                }
            }
        }
        else
        {
            float dist = Vector2.Distance(transform.position, playerTarget.position);

            // Lost player — too far away
            if (dist > detectionRange * 1.5f)
            {
                playerTarget = null;
                currentState = (patrolPoints != null && patrolPoints.Length > 0)
                    ? AIState.Patrol : AIState.Idle;
            }
        }
    }

    // ════════════════════════════════════════════
    //  AI STATES
    // ════════════════════════════════════════════

    void UpdateIdle()
    {
        PlayAnim(idleAnim);

        // Stationary enemies attack from idle if player is in attack range
        if (attacksPlayer && playerTarget != null && aiBehavior == AIBehavior.Stationary)
        {
            float dist = Vector2.Distance(transform.position, playerTarget.position);
            if (dist <= attackRange && attackTimer <= 0)
            {
                FaceTarget(playerTarget.position);
                currentState = AIState.Attack;
            }
        }
    }

    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentState = AIState.Idle;
            return;
        }

        // Wait at point
        if (isWaiting)
        {
            PlayAnim(idleAnim);
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0) isWaiting = false;
            return;
        }

        PlayAnim(walkAnim);

        Transform target = patrolPoints[patrolIndex];
        float speed = patrolSpeed * GetSpeedMultiplier();

        // Face movement direction
        FaceTarget(target.position);

        // Move
        if (aiBehavior == AIBehavior.FlyingPatrol || aiBehavior == AIBehavior.FlyingAggressive)
        {
            // Fly directly to point (X and Y)
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        }
        else
        {
            // Walk on ground (X only)
            Vector2 dest = new Vector2(target.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, dest, speed * Time.deltaTime);
        }

        // Reached point
        float threshold = (aiBehavior == AIBehavior.FlyingPatrol || aiBehavior == AIBehavior.FlyingAggressive) ? 0.1f : 0.3f;
        if (Vector2.Distance(transform.position, target.position) < threshold)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            if (waitAtPoints)
            {
                isWaiting = true;
                waitTimer = waitDuration;
            }
        }
    }

    void UpdateChase()
    {
        if (playerTarget == null)
        {
            currentState = AIState.Idle;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        // In attack range
        if (dist <= attackRange && attackTimer <= 0)
        {
            currentState = AIState.Attack;
            return;
        }

        // Close enough — wait for cooldown, don't walk into player
        if (dist <= attackRange)
        {
            PlayAnim(idleAnim);
            FaceTarget(playerTarget.position);
            return;
        }

        // Chase
        PlayAnim(walkAnim);
        FaceTarget(playerTarget.position);

        float speed = chaseSpeed * GetSpeedMultiplier();
        if (aiBehavior == AIBehavior.FlyingAggressive)
        {
            // Only move closer if not already in attack range
            if (dist > attackRange * 0.9f)
            {
                transform.position = Vector2.MoveTowards(transform.position, playerTarget.position, speed * Time.deltaTime);
            }
        }
        else
        {
            // Walk on ground (X only)
            Vector2 dest = new Vector2(playerTarget.position.x, transform.position.y);
            transform.position = Vector2.MoveTowards(transform.position, dest, speed * Time.deltaTime);
        }
    }

    void UpdateAttack()
    {
        isAttacking = true;
        StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        PlayAnim(attackAnim);
        PlaySFX(attackSFX);

        attackTimer = attackCooldown;

        // Wait for attack animation to play
        yield return null; // let animator start
        float attackDuration = 0.5f;
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0) attackDuration = info.length;
        }

        // Deal damage partway through the animation
        yield return new WaitForSeconds(attackDuration * 0.4f);
        if (!isDead && playerTarget != null)
        {
            float dist = Vector2.Distance(transform.position, playerTarget.position);
            if (dist <= attackRange * 1.5f)
            {
                Player player = playerTarget.GetComponent<Player>();
                if (player != null)
                    player.TakeDamage(attackDamage);
            }
        }

        // Wait for rest of animation
        yield return new WaitForSeconds(attackDuration * 0.5f);

        if (isDead) { isAttacking = false; yield break; }

        isAttacking = false;

        // Now transition to next state
        if (retreatAfterAttack && aiBehavior != AIBehavior.Stationary)
        {
            currentState = AIState.Retreat;
            retreatTimer = retreatDuration;
        }
        else
        {
            currentState = (playerTarget != null) ? AIState.Chase : AIState.Idle;
            if (aiBehavior == AIBehavior.Stationary)
                currentState = AIState.Idle;
        }
    }

    void UpdateRetreat()
    {
        PlayAnim(walkAnim);
        retreatTimer -= Time.deltaTime;

        if (playerTarget != null)
        {
            FaceTarget(playerTarget.position);
            float dir = transform.position.x > playerTarget.position.x ? 1f : -1f;
            float speed = chaseSpeed * 0.5f * GetSpeedMultiplier();
            transform.position += new Vector3(dir * speed * Time.deltaTime, 0, 0);
        }

        if (retreatTimer <= 0)
        {
            currentState = (playerTarget != null) ? AIState.Chase : AIState.Idle;
        }
    }

    // ════════════════════════════════════════════
    //  COMBAT
    // ════════════════════════════════════════════

    IEnumerator DealDamageDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isDead || playerTarget == null) yield break;

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        if (dist <= attackRange * 1.5f)
        {
            Player player = playerTarget.GetComponent<Player>();
            if (player != null)
                player.TakeDamage(attackDamage);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!contactDamage || isDead) return;
        if (contactCooldownTimer > 0) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(contactDamageAmount);
                contactCooldownTimer = 0.5f; // prevent rapid hits
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Also handle trigger-based contact damage
        if (!contactDamage || isDead) return;
        if (contactCooldownTimer > 0) return;

        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(contactDamageAmount);
                contactCooldownTimer = 0.5f;
            }
        }
    }

    // ════════════════════════════════════════════
    //  DAMAGE / DEATH
    // ════════════════════════════════════════════

    void OnDamaged()
    {
        UpdateHealthBar();
        PlaySFX(hurtSFX);

        // Show health bar on first hit (mobs)
        if (!healthBarVisible && enemyType == EnemyType.Mob)
        {
            healthBarVisible = true;
            if (uiRoot != null) uiRoot.SetActive(true);
        }

        // Hurt flash
        if (spriteRenderer != null)
            StartCoroutine(DamageFlash());

        // Play hurt animation briefly
        if (!string.IsNullOrEmpty(hurtAnim) && animator != null)
        {
            PlayAnim(hurtAnim);
            StartCoroutine(ResumeAfterHurt());
        }
    }

    IEnumerator ResumeAfterHurt()
    {
        yield return null; // let animator start
        float duration = 0.3f;
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0) duration = info.length * 0.8f;
        }
        yield return new WaitForSeconds(duration);
        if (!isDead && !isAttacking)
            PlayAnim(idleAnim);
    }

    void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        currentState = AIState.Dead;

        PlayAnim(deathAnim);
        PlaySFX(deathSFX);

        // Freeze animator on death so it doesn't transition back to idle
        if (animator != null && !string.IsNullOrEmpty(deathAnim))
            StartCoroutine(FreezeAfterDeath());

        // Notify GameManager (XP + chaos)
        if (GameManager.Instance != null)
            GameManager.Instance.OnEnemyKilled(xpReward);

        // Gold drop
        if (goldDrop > 0 && GameManager.Instance != null)
            GameManager.Instance.AddCoins(goldDrop);

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable collider so player can walk through
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Cleanup
        if (uiRoot != null) Destroy(uiRoot);
        Destroy(gameObject, 1.2f);
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = isSlowed ? new Color(0.5f, 0.8f, 1f) : originalColor;
    }

    IEnumerator FreezeAfterDeath()
    {
        // Wait a frame for death animation to start playing
        yield return null;
        // Get death animation length and freeze before it can loop back
        float length = 0.8f;
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0) length = info.length;
        }
        yield return new WaitForSeconds(length * 0.9f);
        if (animator != null)
            animator.enabled = false;
    }

    // ════════════════════════════════════════════
    //  SLOW EFFECT (for ice bolt etc.)
    // ════════════════════════════════════════════

    public void ApplySlow(float duration)
    {
        isSlowed = true;
        slowTimer = duration;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.5f, 0.8f, 1f, 1f);
    }

    void RemoveSlow()
    {
        isSlowed = false;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    public float GetSpeedMultiplier() => isSlowed ? 0.4f : 1f;
    public bool IsDead => isDead;

    // ════════════════════════════════════════════
    //  UI (Health Bar + Name Label)
    // ════════════════════════════════════════════

    void CreateUI()
    {
        uiRoot = new GameObject($"{enemyName}_UI");

        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform canvasRt = uiRoot.GetComponent<RectTransform>();
        float barWidthWorld = enemyType == EnemyType.Boss ? 3f : 2f;
        float barHeightWorld = enemyType == EnemyType.Boss ? 0.35f : 0.25f;
        // sizeDelta is in canvas pixels; at 0.01 scale, 100px = 1 world unit
        float barWidth = barWidthWorld * 100f;
        float barHeight = barHeightWorld * 100f;
        float labelHeight = nameLabelHeight * 100f;
        canvasRt.sizeDelta = new Vector2(barWidth, barHeight + labelHeight);
        canvasRt.localScale = Vector3.one * 0.01f;

        // ── Name Label (always visible) ──
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(uiRoot.transform, false);
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = enemyName;
        nameText.fontSize = enemyType == EnemyType.Boss ? 24 : 18;
        nameText.color = enemyType == EnemyType.Boss
            ? new Color(1f, 0.4f, 0.3f)
            : new Color(0.9f, 0.85f, 0.75f);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = enemyType == EnemyType.Boss ? FontStyles.Bold : FontStyles.Normal;

        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0.5f, 0);
        nameRt.anchoredPosition = new Vector2(0, 2);
        nameRt.sizeDelta = new Vector2(0, labelHeight);

        // ── Health Bar Background ──
        GameObject bgObj = new GameObject("HealthBg");
        bgObj.transform.SetParent(uiRoot.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0);
        bgRt.anchorMax = new Vector2(1, 0);
        bgRt.pivot = new Vector2(0.5f, 0);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(0, barHeight);

        // ── Health Bar Trail (ghost bar that fades behind) ──
        GameObject trailObj = new GameObject("HealthTrail");
        trailObj.transform.SetParent(bgObj.transform, false);
        healthBarTrail = trailObj.AddComponent<Image>();
        healthBarTrail.color = new Color(0.9f, 0.3f, 0.2f, 0.5f);
        trailRt = trailObj.GetComponent<RectTransform>();
        trailRt.anchorMin = Vector2.zero;
        trailRt.anchorMax = Vector2.one;
        trailRt.offsetMin = new Vector2(2, 2);
        trailRt.offsetMax = new Vector2(-2, -2);

        // ── Health Bar Fill ──
        GameObject fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = enemyType == EnemyType.Boss
            ? new Color(0.9f, 0.15f, 0.1f)
            : new Color(0.85f, 0.25f, 0.25f);
        fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);

        // Boss: always visible. Mob: hidden until damaged.
        if (enemyType == EnemyType.Mob)
        {
            healthBarVisible = false;
            bgObj.SetActive(false);
        }
        else
        {
            healthBarVisible = true;
        }
    }

    void UpdateUI()
    {
        if (uiRoot == null) return;
        uiRoot.transform.position = transform.position + healthBarOffset;

        // Smooth health bar animation via anchor
        float speed = Time.deltaTime * 5f;
        if (fillRt != null)
        {
            float current = fillRt.anchorMax.x;
            float next = Mathf.Lerp(current, healthBarTarget, speed);
            fillRt.anchorMax = new Vector2(next, 1f);
            fillRt.offsetMax = new Vector2(-2, -2);
        }
        if (trailRt != null)
        {
            healthBarTrailValue = Mathf.Lerp(healthBarTrailValue, healthBarTarget, speed * 0.4f);
            trailRt.anchorMax = new Vector2(healthBarTrailValue, 1f);
            trailRt.offsetMax = new Vector2(-2, -2);
        }
    }

    void UpdateHealthBar()
    {
        if (healthBarFill == null || health == null) return;
        healthBarTarget = (float)health.currentHealth / health.maxHealth;

        // Show health bar bg for mobs when damaged
        if (enemyType == EnemyType.Mob && !healthBarVisible)
        {
            healthBarVisible = true;
            Transform bg = uiRoot.transform.Find("HealthBg");
            if (bg != null) bg.gameObject.SetActive(true);
        }
    }

    // ════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════

    void FaceTarget(Vector3 target)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (target.x - transform.position.x) < 0f;
        }
    }

    void PlayAnim(string animName)
    {
        if (animator == null || string.IsNullOrEmpty(animName)) return;

        // Check if this animation exists before playing
        if (animator.HasState(0, Animator.StringToHash(animName)))
        {
            animator.Play(animName);
        }
    }

    void PlaySFX(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, sfxVolume);
    }

    // ════════════════════════════════════════════
    //  GIZMOS (visual helpers in Scene view)
    // ════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        // Detection range (yellow wire sphere)
        if (attacksPlayer)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        // Attack range (red wire sphere)
        if (attacksPlayer)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        // Patrol path (green lines)
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.7f);
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                int next = (i + 1) % patrolPoints.Length;
                if (patrolPoints[next] == null) continue;

                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
                Gizmos.DrawSphere(patrolPoints[i].position, 0.15f);
            }
        }

        // Health bar position preview
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Vector3 barPos = transform.position + healthBarOffset;
        float barW = enemyType == EnemyType.Boss ? 1.5f : 0.75f;
        Gizmos.DrawWireCube(barPos, new Vector3(barW, 0.15f, 0f));
    }

    // ════════════════════════════════════════════
    //  CLEANUP
    // ════════════════════════════════════════════

    void OnDestroy()
    {
        if (uiRoot != null)
            Destroy(uiRoot);
    }
}
