using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// PATCH 5 — MAJOR CHANGES:
/// 
/// CHAOS MODE (activated via K when meter full, managed by GameManager):
///   - Sprite changes to darkModeSprite (with transition sprite in between)
///   - 3x melee damage
///   - Faster attack speed (0.4s cooldown vs 0.8s)
///   - 1.5x move speed
///   - Triple jump (2 air jumps) + 1.5x jump force
///   - Lasts 10 seconds then reverts
///
/// STAT-BASED DAMAGE:
///   - Melee now uses PlayerStats.RollMeleeDamage (STR scaling + LUK crit)
///   - Dark mode multiplies the rolled damage by 3
///
/// SFX SYSTEM:
///   - All sound effects assignable in Inspector
///   - Separate clips for human/dark mode walking, attacking, dying
///   - Jump SFX, transition SFX
///   - Footstep system with interval timer
///
/// SPRITE SETUP (Inspector):
///   1. Assign "Normal Sprite" = your default player sprite
///   2. Assign "Dark Mode Sprite" = chaos/dark form sprite
///   3. Assign "Transition Sprite" = sprite shown briefly during transform (0.3s)
///   4. If using Animator, the Animator will override these during gameplay.
///      For static sprites (no animation), leave Animator off.
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody2D rb;
    public PlayerInput playerInput;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;
    [SerializeField] private TrailRenderer tr;   // from original

    #region PLAYER STATS
    [Header("Player Stats")]
    public int coins;  
    public int money;          // kept from original (Yigit uses GameManager)
    public int facingDirection = 1;
    #endregion
    

    #region MOVEMENT SETTINGS
    [Header("Movement Settings")]
    public float speed = 5f;
    public Vector2 moveInput;
    public bool IsFacingRight => facingDirection == 1;

    [Header("Jump")]
    public float jumpForce = 10f;
    public float jumpCutMultiplier = 1f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    [Tooltip("Air jumps in normal mode. 1 = double jump.")]
    public int maxAirJumps = 1;
    private bool doubleJumpUnlocked = false;  // Start with double jump LOCKED

    [Header("Combat")]
    public float attackRadius = 0.5f;
    public Transform attackPoint;
    public LayerMask enemyLayer;
    #endregion

    [Header("Hit Effects")]
    public GameObject impactFXPrefab;       // Particle system for enemy hits (with child "Splash")
    public GameObject wallImpactFXPrefab;   // Particle system for wall hits
    public LayerMask wallLayer;             // Assign the layer your walls are on (should be "Ground")

    [Header("Knockback & Screenshake")]
    public float knockbackForce = 5f;       // Force applied to enemies when hit
    

    #region DARK MODE SPRITES
    [Header("Dark Mode Sprites")]
    [Tooltip("Default player sprite. If blank, uses whatever is on SpriteRenderer at Start.")]
    public Sprite normalSprite;
    [Tooltip("Chaos/dark mode sprite. Assign your dark form sprite here.")]
    public Sprite darkModeSprite;
    [Tooltip("First transition sprite shown briefly when transforming. Optional.")]
    public Sprite transitionSprite;
    [Tooltip("Second transition sprite shown after the first. Optional.")]
    public Sprite transitionSprite2;
    [Tooltip("Animator Override Controller with dark mode animations. Created via Create → Animator Override Controller.")]
    public AnimatorOverrideController darkAnimOverride;

    [Header("Dark Mode Config")]
    public int darkModeDamageMultiplier = 3;
    public float darkModeSpeedMultiplier = 1.5f;
    public float darkModeJumpMultiplier = 1.5f;
    public int darkModeAirJumps = 2; // triple jump
    [Tooltip("Attack cooldown in normal form (seconds).")]
    public float normalAttackCooldown = 0.8f;
    [Tooltip("Attack cooldown in dark mode (seconds). Must be >= dark_attack animation length or frames get cut off.")]
    public float darkAttackCooldown = 1.1f;
    #endregion

    #region DASH SETTINGS
    [Header("Dash Settings")]
    public float dashPower = 24f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;
    #endregion

    #region SCALE
    [Header("Scale")]
    public float baseScale = 1f;
    #endregion

    #region SFX
    [Header("=== SFX (assign in Inspector) ===")]
    [Tooltip("Footstep sound in normal form")]
    public AudioClip walkSFX;
    [Tooltip("Footstep sound in dark/chaos form")]
    public AudioClip darkWalkSFX;
    [Tooltip("Attack/swing sound in normal form")]
    public AudioClip attackSFX;
    [Tooltip("Attack sound in dark/chaos form")]
    public AudioClip darkAttackSFX;
    [Tooltip("Jump sound (both forms)")]
    public AudioClip jumpSFX;
    [Tooltip("Death sound in normal form")]
    public AudioClip dieSFX;
    [Tooltip("Death sound in dark/chaos form")]
    public AudioClip darkDieSFX;
    [Tooltip("Sound when transforming into/out of chaos mode")]
    public AudioClip transformSFX;

    [Header("SFX Settings")]
    public float footstepInterval = 0.35f;
    public float sfxVolume = 0.6f;
    #endregion

    // --- Private ---
    private bool isGrounded;
    private bool wasGrounded;
    private int airJumpsLeft;
    private bool usedGroundJump;
    private RuntimeAnimatorController originalAnimController;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private bool inputEnabled = true;
    private bool darkModeActive = false;
    private Sprite _originalSprite;

    // Footstep timer
    private float footstepTimer = 0f;
    private bool wasMoving = false;
    private bool movementEnabled = true;

    // Transition
    private bool isTransitioning = false;

    // Legacy compatibility
    [HideInInspector] public int extraJumpsValue = -1;
    [HideInInspector] public int maxHealth = 100;
    [HideInInspector] public int damage = 10; // legacy

    // Health through GameManager
    private int health
    {
        get => GameManager.Instance != null ? GameManager.Instance.playerHealth : 100;
        set { if (GameManager.Instance != null) GameManager.Instance.SetPlayerHealth(value); }
    }

    // Current max health from stats
    private int currentMaxHealth => GameManager.Instance != null ? GameManager.Instance.playerMaxHealth : 100;

    // Screenshake
    private Transform cameraTransform;
    private Vector3 originalCameraPos;

    // Prefab scales
    private Vector3 originalImpactScale;
    private Vector3 originalWallImpactScale;

    #region UNITY LIFE CYCLE METHODS
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator != null) originalAnimController = animator.runtimeAnimatorController;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        airJumpsLeft = 0; // Start with no air jumps
        usedGroundJump = false;

        if (spriteRenderer != null)
        {
            _originalSprite = spriteRenderer.sprite;
            if (normalSprite == null) normalSprite = _originalSprite;
        }

        if (GameManager.Instance != null)
        {
            Vector2? savedPos = GameManager.Instance.GetSavedPosition();
            if (savedPos.HasValue)
                transform.position = savedPos.Value;

            facingDirection = GameManager.Instance.facingDirection;
            float s = baseScale;
            transform.localScale = new Vector3(s * facingDirection, s, s);

            if (GameManager.Instance.isDarkMode)
                ApplyDarkModeVisuals();

            UIManager.Instance?.RefreshAllDisplays();
        }

        // Cache camera transform for screenshake
        Camera mainCam = Camera.main;
        if (mainCam != null)
            cameraTransform = mainCam.transform;

        // Store original scales from prefabs
        if (impactFXPrefab != null)
            originalImpactScale = impactFXPrefab.transform.localScale;
        if (wallImpactFXPrefab != null)
            originalWallImpactScale = wallImpactFXPrefab.transform.localScale;
    }

    void Update()
    {
        if (!inputEnabled || isDashing) return; // dashing blocks normal updates
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        Flip();

        // Ground check
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            airJumpsLeft = doubleJumpUnlocked ? GetCurrentAirJumps() : 0;
            usedGroundJump = false;
            canDash = true; // reset dash when grounded (from original)
        }

        SetAnimation(moveInput.x);

        // Health display
        if (GameManager.Instance != null)
            UIManager.Instance?.UpdateHealthBar((float)health / currentMaxHealth);

        // Footstep SFX
        HandleFootsteps();

        // Quick Save (F5)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            GameManager.Instance?.QuickSave();
            UIManager.Instance?.ShowSaveNotification("Quick Saved!");
        }

        // Jump cut
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    void FixedUpdate()
    {
        if (!inputEnabled || isDashing) return;
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        float currentSpeed = speed;
        if (darkModeActive) currentSpeed *= darkModeSpeedMultiplier;

        if (!isAttacking)
            rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    #endregion

    #region INPUT
    void Flip()
    {
        bool wasFacingRight = facingDirection == 1;
        
        if (moveInput.x > 0.1f) facingDirection = 1;
        else if (moveInput.x < -0.1f) facingDirection = -1;
        
        bool isNowFacingRight = facingDirection == 1;
        
        // Only flip if direction actually changed
        if (wasFacingRight != isNowFacingRight)
        {
            float currentScale = baseScale;
            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                transform.localScale = new Vector3(currentScale * facingDirection, currentScale, currentScale);
                
                if (GameManager.Instance != null)
                    GameManager.Instance.facingDirection = facingDirection;
            }
        }
    }

    public void OnMove(InputValue value)
    {
        if (!inputEnabled || !movementEnabled) { moveInput = Vector2.zero; return; }
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!inputEnabled || !movementEnabled || !value.isPressed || isAttacking || isDashing) return;

        float currentJumpForce = jumpForce;
        if (darkModeActive) currentJumpForce *= darkModeJumpMultiplier;

        // Dev infinite jump
        if (DevPanel.Instance != null && DevPanel.Instance.infiniteJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
            PlaySound(jumpSFX);
            return;
        }

        // Ground jump (always allowed)
        if (isGrounded && !usedGroundJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
            usedGroundJump = true;
            PlaySound(jumpSFX);
            return;
        }

        // Air jump (double jump) - ONLY if unlocked
        if (!isGrounded && airJumpsLeft > 0 && doubleJumpUnlocked)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
            airJumpsLeft--;
            PlaySound(jumpSFX);
            return;
        }
    }

    public void OnDash(InputValue value)
    {
        if (!inputEnabled || !movementEnabled || isAttacking) return;
        if (value.isPressed && canDash)
        {
            StartCoroutine(Dash());
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb.gravityScale;
        float originalSpeed = speed;

        rb.gravityScale = 0f;
        speed = 0f;
        rb.linearVelocity = new Vector2(facingDirection * dashPower, 0f);

        if (tr != null)
            tr.emitting = true;

        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = originalGravity;
        speed = originalSpeed;

        if (tr != null)
            tr.emitting = false;

        spriteRenderer.color = Color.white;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void OnAttack(InputValue value)
    {
        if (!inputEnabled || !movementEnabled) return;
        float currentCooldown = darkModeActive ? darkAttackCooldown : normalAttackCooldown;
        if (value.isPressed && !isAttacking && !isDashing && Time.time >= lastAttackTime + currentCooldown)
        {
            StartAttack();
        }
    }
    #endregion

    #region COMBAT
    void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator != null)
            animator.Play("player_attack", 0, 0f);

        // Activate spellFX only in normal (human) form
        if (!darkModeActive)
        {
            Transform spellFX = transform.Find("spellFX");
            if (spellFX != null)
            {
                spellFX.gameObject.SetActive(true);
                Animator fxAnim = spellFX.GetComponent<Animator>();
                if (fxAnim != null) fxAnim.Play("hitFX", 0, 0f);
            }
        }

        PlaySound(darkModeActive ? darkAttackSFX : attackSFX);

        StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        yield return null;

        float clipLength = 0.5f;
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0) clipLength = info.length;
        }

        float damageDelay = clipLength * 0.4f;
        yield return new WaitForSeconds(damageDelay);
        DealAttackDamage();

        yield return new WaitForSeconds(clipLength - damageDelay);

        isAttacking = false;
        transform.Find("spellFX")?.gameObject.SetActive(false);
    }

    void DealAttackDamage()
    {
        int dmg = 10;
        bool crit = false;
        if (GameManager.Instance != null)
            dmg = GameManager.Instance.stats.RollMeleeDamage(out crit);

        if (darkModeActive)
            dmg *= darkModeDamageMultiplier;

        // Check both enemies and walls in one overlap
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer | wallLayer);
        
        bool hitEnemy = false;
        bool hitWall = false;
        Vector2 closestHitPoint = attackPoint.position; // Default to attack point

        foreach (Collider2D hit in hits)
        {
            // Check if it's an enemy
            Health enemyHealth = hit.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-dmg);
                hitEnemy = true;
                if (crit)
                    UIManager.Instance?.ShowCritPopup();

                // Apply knockback to enemy
                Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                    enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
                }
                
                // Get the closest point on the enemy's collider to the attack point
                closestHitPoint = hit.ClosestPoint(attackPoint.position);
            }
            // Check if it's a wall
            else if (((1 << hit.gameObject.layer) & wallLayer) != 0)
            {
                hitWall = true;
                // Get the closest point on the wall's collider to the attack point
                closestHitPoint = hit.ClosestPoint(attackPoint.position);
            }
        }

        // Spawn effect based on what we hit - at the EXACT hit point
        if (hitEnemy)
        {
            SpawnEffect(impactFXPrefab, closestHitPoint, originalImpactScale);
        }
        else if (hitWall)
        {
            SpawnEffect(wallImpactFXPrefab, closestHitPoint, originalWallImpactScale);
        }
        // If neither, do nothing (air swing)
    }

    // Helper method to spawn and auto-destroy particle effects with correct scale
    void SpawnEffect(GameObject prefab, Vector3 position, Vector3 originalScale)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Impact effect prefab is not assigned in the Inspector!");
            return;
        }

        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        
        // Force the exact scale from the prefab
        effect.transform.localScale = originalScale;
        
        // Get the particle system to determine how long to wait before destroying
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float duration = ps.main.duration;
            Destroy(effect, duration);
        }
        else
        {
            // If no particle system, destroy after 0.5 seconds as fallback
            Destroy(effect, 0.5f);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDashing) return; // invincibility during dash (from original)

        health -= damageAmount;
        StartCoroutine(BlinkRed());
        if (health <= 0) Die();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage") && !isDashing)
        {
            TakeDamage(25);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private IEnumerator BlinkRed()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        PlaySound(darkModeActive ? darkDieSFX : dieSFX, 1f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerHealth = currentMaxHealth;
            if (darkModeActive)
                GameManager.Instance.DeactivateDarkMode();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    #endregion

    #region DARK MODE
    private int GetCurrentAirJumps()
    {
        if (!doubleJumpUnlocked) return 0;  // No air jumps until unlocked
        return darkModeActive ? darkModeAirJumps : maxAirJumps;
    }

    public void ActivateDarkMode()
    {
        StartCoroutine(TransformSequence(true));
    }

    public void DeactivateDarkMode()
    {
        StartCoroutine(TransformSequence(false));
    }

    private IEnumerator TransformSequence(bool toDark)
    {
        PlaySound(transformSFX, 0.8f);

        isTransitioning = true;

        if (animator != null) animator.enabled = false;

        if (transitionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = transitionSprite;
            spriteRenderer.color = new Color(1f, 0.6f, 1f, 1f);
            yield return new WaitForSeconds(0.15f);
        }

        if (transitionSprite2 != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = transitionSprite2;
            spriteRenderer.color = new Color(0.8f, 0.4f, 1f, 1f);
            yield return new WaitForSeconds(0.15f);
        }
        else if (transitionSprite != null && spriteRenderer != null)
        {
            yield return new WaitForSeconds(0.15f);
        }

        isTransitioning = false;

        if (toDark)
            ApplyDarkModeVisuals();
        else
            RemoveDarkModeVisuals();

        if (animator != null) animator.enabled = true;
    }

    private void ApplyDarkModeVisuals()
    {
        darkModeActive = true;
        airJumpsLeft = darkModeAirJumps;

        if (darkAnimOverride != null && animator != null)
            animator.runtimeAnimatorController = darkAnimOverride;

        if (darkModeSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = darkModeSprite;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private void RemoveDarkModeVisuals()
    {
        darkModeActive = false;
        airJumpsLeft = maxAirJumps;

        if (originalAnimController != null && animator != null)
            animator.runtimeAnimatorController = originalAnimController;

        if (normalSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = normalSprite;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (GameManager.Instance != null)
            GameManager.Instance.isDarkMode = false;
    }

    public bool IsDarkModeActive => darkModeActive;
    #endregion

    #region DOUBLE JUMP UNLOCK
    public void UnlockDoubleJump()
    {
        doubleJumpUnlocked = true;
        airJumpsLeft = GetCurrentAirJumps();
        Debug.Log("Double jump unlocked!");
    }

    public bool HasDoubleJump() => doubleJumpUnlocked;
    #endregion

    #region SFX
    private void HandleFootsteps()
    {
        bool isMoving = isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isAttacking && !isDashing;

        if (isMoving)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                AudioClip clip = darkModeActive ? darkWalkSFX : walkSFX;
                PlaySound(clip, sfxVolume * 0.5f);
                float interval = darkModeActive ? footstepInterval * 0.7f : footstepInterval;
                footstepTimer = interval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
        wasMoving = isMoving;
    }

    private void PlaySound(AudioClip clip, float volume = -1f)
    {
        if (audioSource == null || clip == null) return;
        if (volume < 0) volume = sfxVolume;
        audioSource.PlayOneShot(clip, volume);
    }

    public void PlaySFX(AudioClip audioClip, float volume = 1f, float pitch = 1.5f)
    {
        if (audioSource == null || audioClip == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(audioClip, volume);
        audioSource.pitch = 1f;
    }
    #endregion

    #region COINS / DIALOGUE
    public void AddMoney(int amount)
    {
        money += amount;
        if (CurrencyController.Instance != null)
            CurrencyController.Instance.AddCurrency(amount);
        else if (GameManager.Instance != null)
            GameManager.Instance.AddMoney(amount);
    }

    public void AddCoins(int amount)
    {
        AddMoney(amount);
    }

    public void Heal(int amount)
    {
        health += amount;
        if (health > currentMaxHealth)
        {
            health = currentMaxHealth;
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerHealth = health;
        }
        
        UIManager.Instance?.UpdateHealthBar((float)health / currentMaxHealth);
        Debug.Log($"Healed {amount} health. Current health: {health}");
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void DisableMovement()
    {
        movementEnabled = false;
        moveInput = Vector2.zero;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        Debug.Log("Player movement disabled");
    }

    public void EnableMovement()
    {
        movementEnabled = true;
        Debug.Log("Player movement enabled");
    }
    #endregion

    #region ANIMATION
    private void SetAnimation(float moveInput)
    {
        if (animator == null || isAttacking || isTransitioning || isDashing) return;

        if (isGrounded)
        {
            if (moveInput == 0) animator.Play("player_idle");
            else animator.Play("player_run");
        }
        else
        {
            if (rb.linearVelocity.y > 0) animator.Play("player_jump");
            else animator.Play("player_fall");
        }
    }
    #endregion

    #region GIZMOS
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
    #endregion

}