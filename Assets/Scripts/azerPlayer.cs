using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    public float speed = 5f;
    public int facingDirection = 1;
    public Vector2 moveInput;

    [Header("Jump")]
    public float jumpForce = 10f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    [Tooltip("Air jumps in normal mode. 1 = double jump.")]
    public int maxAirJumps = 1;

    [Header("Combat")]
    public float attackRadius = 0.5f;
    public Transform attackPoint;
    public LayerMask enemyLayer;
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

    [Header("Scale")]
    public float baseScale = 4f;

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

    // --- Private ---
    private bool isGrounded;
    private bool wasGrounded;
    private int airJumpsLeft;
    private bool usedGroundJump;
    private Animator animator;
    private RuntimeAnimatorController originalAnimController;
    private SpriteRenderer spriteRenderer;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private AudioSource audioSource;
    private bool inputEnabled = true;
    private bool darkModeActive = false;
    private Sprite _originalSprite;

    // Footstep timer
    private float footstepTimer = 0f;
    private bool wasMoving = false;

    // Transition
    private bool isTransitioning = false;

    [HideInInspector] public int extraJumpsValue = -1; // legacy
    [HideInInspector] public int maxHealth = 100; // legacy, now from stats
    [HideInInspector] public int damage = 10; // legacy, now from stats

    // Health through GameManager
    private int health
    {
        get => GameManager.Instance != null ? GameManager.Instance.playerHealth : 100;
        set { if (GameManager.Instance != null) GameManager.Instance.SetPlayerHealth(value); }
    }

    // Current max health from stats
    private int currentMaxHealth => GameManager.Instance != null ? GameManager.Instance.playerMaxHealth : 100;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator != null) originalAnimController = animator.runtimeAnimatorController;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        airJumpsLeft = maxAirJumps;
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
    }

    void Update()
    {
        if (!inputEnabled) return;
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        Flip();

        // Ground check
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            airJumpsLeft = GetCurrentAirJumps();
            usedGroundJump = false;
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
    }

    void FixedUpdate()
    {
        if (!inputEnabled) return;
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        float currentSpeed = speed;
        if (darkModeActive) currentSpeed *= darkModeSpeedMultiplier;

        if (!isAttacking)
            rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    // ============ INPUT ============

    void Flip()
    {
        if (moveInput.x > 0.1f) facingDirection = 1;
        else if (moveInput.x < -0.1f) facingDirection = -1;

        float currentScale = baseScale;
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            transform.localScale = new Vector3(currentScale * facingDirection, currentScale, currentScale);
            if (GameManager.Instance != null)
                GameManager.Instance.facingDirection = facingDirection;
        }
    }

    public void OnMove(InputValue value)
    {
        if (!inputEnabled) { moveInput = Vector2.zero; return; }
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!inputEnabled || !value.isPressed || isAttacking) return;

        float currentJumpForce = jumpForce;
        if (darkModeActive) currentJumpForce *= darkModeJumpMultiplier;

        // Dev infinite jump
        if (DevPanel.Instance != null && DevPanel.Instance.infiniteJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
            PlaySound(jumpSFX);
            return;
        }

        if (isGrounded && !usedGroundJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
            usedGroundJump = true;
            PlaySound(jumpSFX);
            return;
        }

        if (!isGrounded && airJumpsLeft > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
            airJumpsLeft--;
            PlaySound(jumpSFX);
            return;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!inputEnabled) return;
        float currentCooldown = darkModeActive ? darkAttackCooldown : normalAttackCooldown;
        if (value.isPressed && !isAttacking && Time.time >= lastAttackTime + currentCooldown)
        {
            StartAttack();
        }
    }

    // ============ COMBAT ============

    void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Play attack animation directly (works with Override Controller)
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

        // SFX
        PlaySound(darkModeActive ? darkAttackSFX : attackSFX);

        // Start coroutine to wait for animation to finish, then deal damage and end attack
        StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        // Wait one frame for animator to start the clip
        yield return null;

        // Get actual clip length from animator
        float clipLength = 0.5f; // fallback
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0) clipLength = info.length;
        }

        // Deal damage at 40% through animation
        float damageDelay = clipLength * 0.4f;
        yield return new WaitForSeconds(damageDelay);
        DealAttackDamage();

        // Wait for rest of animation
        yield return new WaitForSeconds(clipLength - damageDelay);

        // End attack
        isAttacking = false;

        // Deactivate spellFX
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

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        foreach (Collider2D enemy in enemies)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-dmg);
                if (crit)
                    UIManager.Instance?.ShowCritPopup();
            }
        }
    }

    // ============ DAMAGE / DEATH ============

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        StartCoroutine(BlinkRed());
        if (health <= 0) Die();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
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
            // Reset health to max
            GameManager.Instance.playerHealth = currentMaxHealth;
            // End dark mode if active
            if (darkModeActive)
                GameManager.Instance.DeactivateDarkMode();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // ============ DARK / CHAOS MODE ============

    private int GetCurrentAirJumps()
    {
        return darkModeActive ? darkModeAirJumps : maxAirJumps;
    }

    /// <summary>Called by GameManager when chaos mode activates</summary>
    public void ActivateDarkMode()
    {
        StartCoroutine(TransformSequence(true));
    }

    /// <summary>Called by GameManager when chaos mode ends</summary>
    public void DeactivateDarkMode()
    {
        StartCoroutine(TransformSequence(false));
    }

    private IEnumerator TransformSequence(bool toDark)
    {
        PlaySound(transformSFX, 0.8f);

        isTransitioning = true;

        // Pause the animator so transition sprites are visible
        if (animator != null) animator.enabled = false;

        // Show transition sprite 1
        if (transitionSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = transitionSprite;
            spriteRenderer.color = new Color(1f, 0.6f, 1f, 1f);
            yield return new WaitForSeconds(0.15f);
        }

        // Show transition sprite 2
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

        // Re-enable animator (now with the correct controller)
        if (animator != null) animator.enabled = true;
    }

    private void ApplyDarkModeVisuals()
    {
        darkModeActive = true;
        airJumpsLeft = darkModeAirJumps;

        // Swap to dark mode animator (if override assigned)
        if (darkAnimOverride != null && animator != null)
            animator.runtimeAnimatorController = darkAnimOverride;

        // Fallback: if no override, use static dark sprite
        if (darkModeSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = darkModeSprite;

        // Reset color to white so dark sprites show their true colors
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private void RemoveDarkModeVisuals()
    {
        darkModeActive = false;
        airJumpsLeft = maxAirJumps;

        // Swap back to normal animator
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

    // ============ SFX ============

    private void HandleFootsteps()
    {
        bool isMoving = isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isAttacking;

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

    // Legacy method kept for compatibility
    public void PlaySFX(AudioClip audioClip, float volume = 1f, float pitch = 1.5f)
    {
        if (audioSource == null || audioClip == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(audioClip, volume);
        audioSource.pitch = 1f;
    }

    // ============ COINS / DIALOGUE ============

    public void AddCoins(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddCoins(amount);
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

    // ============ ANIMATION ============

    private void SetAnimation(float moveInput)
    {
        if (animator == null || isAttacking || isTransitioning) return;

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
}
