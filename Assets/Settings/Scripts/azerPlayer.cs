using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int health = 100;

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
    public int extraJumpsValue = 1;

    [Header("UI")]
    public Image healthImage;
    public int coins;

    [Header("Attack")]
    public int damage = 10;
    public float baseAttackRadius = 1.0f;
    public float darkAttackRadius = 2.0f;
    public Transform attackPoint;
    public LayerMask enemyLayer;

    [Header("Attack Movement")]
    [Range(0f, 1f)] public float baseAttackMoveMultiplier = 0.75f;
    [Range(0f, 1f)] public float darkAttackMoveMultiplier = 0.50f;                    



    [Tooltip("only allow attack when grounded.")]
    public bool groundedOnlyAttack = false;

    [Tooltip("delay damage so it lines up with the hit frame of the attack animation.")]
    public float attackHitDelay = 0.0f;

    [Header("Dark Form / Transformation")]
    [Tooltip("Current Black Rot meter.")]
    public float rotMeter = 0f;

    [Tooltip("Meter required to enter dark form.")]
    public float rotMeterMax = 100f;

    [Tooltip("Drain per second while in dark form.")]
    public float rotDrainPerSecond = 12f;

    [Tooltip("Optional: allow transforming back even if meter isn't empty.")]
    public bool allowManualRevert = true;

    [Tooltip("Prevent transform spam.")]
    public float transformCooldown = 0.25f;

    [Tooltip("If true, you can only transform while grounded.")]
    public bool groundedOnlyTransform = false;

    [Header("Dark Form Stats")]
    public float darkSpeedMultiplier = 1.25f;
    public float darkJumpMultiplier = 1.10f;
    public int darkDamageBonus = 5;

    [Header("Animator Controllers")]
    public RuntimeAnimatorController baseController;
    public RuntimeAnimatorController darkController;

    [Header("Visuals / animator")]
    [SerializeField] private Transform flipPivot;     // parent used only for left/right
    [SerializeField] private Transform animatorRoot;  // child that gets scaled by animation
    [Tooltip("If you want to force a specific visual scale, set this > 0. Otherwise leave at 0 to use prefab scale.")]
    [SerializeField] private float overrideVisualScale = 0f;

    [Header("Transformation Animation")]
    [Tooltip("State name in the Animator for base -> dark transform.")]
    public string transformToDarkStateName = "azerTransformToDark";
    [Tooltip("State name in the Animator for dark -> base transform.")]
    public string transformToBaseStateName = "azerTransformToBase";

    private bool isTransforming = false;

    private bool isGrounded;
    private int extraJumps;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector3 visualBaseScale;

    private bool isAttacking = false;
    [Tooltip("Used only if you are NOT using animation events to end attack. Keep close to your clip length.")]
    public float attackCooldown = 0.5f;
    private float lastAttackTime = 0f;

    private AudioSource audioSource;

    // Dark form internal state
    public bool isDarkForm = false;
    private float lastTransformTime = -999f;

    // Base stat cache
    private float baseSpeed;
    private float baseJumpForce;
    private int baseDamage;

    // Animator parameter hashes
    private static readonly int XVel = Animator.StringToHash("xVelocity");
    private static readonly int YVel = Animator.StringToHash("yVelocity");
    private static readonly int Grounded = Animator.StringToHash("isGrounded");

    // Attack param
    private static readonly int AttackTrig = Animator.StringToHash("attack");

    private static readonly int TransformTrig = Animator.StringToHash("transform");


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        extraJumps = extraJumpsValue;

        // Cache base stats once at start
        baseSpeed = speed;
        baseJumpForce = jumpForce;
        baseDamage = damage;

        // Find FlipPivot if not assigned
        if (flipPivot == null)
        {
            Transform foundFlip = transform.Find("FlipPivot");
            if (foundFlip != null)
                flipPivot = foundFlip;
        }

        // Find animator under FlipPivot
        if (animatorRoot == null && flipPivot != null)
        {
            Transform foundAnim = flipPivot.Find("animator");
            if (foundAnim != null)
                animatorRoot = foundAnim;
        }

        if (animatorRoot == null)
        {
            Debug.LogError("Player: Missing child named 'animator' under FlipPivot. Assign animatorRoot or ensure hierarchy Player/FlipPivot/animator.");
        }
        else
        {
            animator = animatorRoot.GetComponent<Animator>();
            spriteRenderer = animatorRoot.GetComponent<SpriteRenderer>();

            if (animator == null) Debug.LogError("Player: No Animator found on the 'animator' child.");
            if (spriteRenderer == null) Debug.LogError("Player: No SpriteRenderer found on the 'animator' child.");

            animatorRoot.localPosition = Vector3.zero;

            // Capture base scale ONCE (this is visual base, animations can keyframe Scale.x/y/z on animatorRoot)
            visualBaseScale = animatorRoot.localScale;

            // force a specific scale
            if (overrideVisualScale > 0f)
            {
                visualBaseScale = new Vector3(overrideVisualScale, overrideVisualScale, overrideVisualScale);
                animatorRoot.localScale = visualBaseScale;
            }

            ApplyVisualFacing();

            // auto-detect base controller from the Animator
            if (baseController == null && animator != null)
                baseController = animator.runtimeAnimatorController;
        }

        // Respawn logic
        if (RespawnState.IsRespawning)
        {
            health = maxHealth;
            health = Mathf.Clamp(health, 0, maxHealth);
            UpdateHealthUI();
            StartCoroutine(ClearRespawnFlagNextFrame());
        }
        else
        {
            health = Mathf.Clamp(health, 0, maxHealth);
            UpdateHealthUI();
        }
    }

    private IEnumerator ClearRespawnFlagNextFrame()
    {
        yield return null;
        RespawnState.IsRespawning = false;
    }

    void Update()
    {
        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        Debug.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * 0.2f,
        isGrounded ? Color.green : Color.red);


        if (isGrounded)
            extraJumps = extraJumpsValue;

        UpdateFacingFromInput();
        ApplyVisualFacing();

        // Drain rot meter while in dark form (don’t interrupt a transform)
        if (isDarkForm && !isTransforming)
        {
            rotMeter -= rotDrainPerSecond * Time.deltaTime;
            if (rotMeter <= 0f)
            {
                rotMeter = 0f;

                // Auto-exit when empty
                if (allowManualRevert && !isTransforming)
                    StartCoroutine(DoTransform(toDark: false));
            }
        }
        else
        {
            rotMeter = Mathf.Clamp(rotMeter, 0f, rotMeterMax);
        }

        UpdateAnimatorParams();
    }

    void FixedUpdate()
    {
        if (isTransforming)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float attackMult = isDarkForm ? darkAttackMoveMultiplier : baseAttackMoveMultiplier;
        float mult = isAttacking ? attackMult : 1f;

        float targetSpeed = moveInput.x * speed * mult;
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }



    private void UpdateFacingFromInput()
    {
        if (moveInput.x > 0.1f) facingDirection = 1;
        else if (moveInput.x < -0.1f) facingDirection = -1;
    }

    private void ApplyVisualFacing()
    {
        if (flipPivot == null) return;

        float x = Mathf.Abs(flipPivot.localScale.x) * facingDirection;
        flipPivot.localScale = new Vector3(x, flipPivot.localScale.y, flipPivot.localScale.z);
    }

    public void OnMove(InputValue value)
    {
        if (isTransforming) return;
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        if (isAttacking || isTransforming) return;

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        else if (extraJumps > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            extraJumps--;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        if (isTransforming) return;

        if (groundedOnlyAttack && !isGrounded)
            return;

        if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
            StartAttack();
    }

    public void OnTransform(InputValue value)
    {
        if (!value.isPressed) return;
        if (isTransforming) return;

        if (Time.time < lastTransformTime + transformCooldown)
            return;

        if (groundedOnlyTransform && !isGrounded)
            return;

        if (isAttacking)
            return;

        lastTransformTime = Time.time;

        if (!isDarkForm)
        {
            if (rotMeter >= rotMeterMax)
                StartCoroutine(DoTransform(toDark: true));
        }
        else
        {
            if (allowManualRevert)
                StartCoroutine(DoTransform(toDark: false));
        }
    }

    public void AddRot(float amount)
    {
        if (amount <= 0f) return;
        if (isDarkForm) return;

        rotMeter = Mathf.Clamp(rotMeter + amount, 0f, rotMeterMax);
    }

    private IEnumerator DoTransform(bool toDark)
{
    if (animator == null)
        yield break;

    isTransforming = true;

    // Stop player cleanly
    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    moveInput = Vector2.zero;

    // Play transform animation on the CURRENT controller
    animator.ResetTrigger(AttackTrig);
    animator.SetTrigger(TransformTrig);

    // Decide which state we expect based on direction
    string stateToWaitFor = toDark ? transformToDarkStateName : transformToBaseStateName;

    // FAILSAFE: don't let us get stuck forever if state name/transition is wrong
    float timeout = 1.5f;
    float startTime = Time.time;

    while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateToWaitFor))
    {
        if (Time.time - startTime > timeout)
        {
            Debug.LogError($"Transform failed: never entered state '{stateToWaitFor}'. " +
                           $"Check Animator state name + Any State transition + trigger 'transform'.");
            isTransforming = false;
            yield break;
        }
        yield return null;
    }

    while (animator.GetCurrentAnimatorStateInfo(0).IsName(stateToWaitFor) &&
           animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
    {
        yield return null;
    }

    // Swap controllers after animation finishes
    if (toDark) ApplyDarkFormNow();
    else ApplyBaseFormNow();

    isTransforming = false;
}


    private void ApplyDarkFormNow()
    {
        isDarkForm = true;

        // Swap animator controller (base -> dark)
        if (animator != null && darkController != null)
            animator.runtimeAnimatorController = darkController;

        // Apply stat buffs
        speed = baseSpeed * darkSpeedMultiplier;
        jumpForce = baseJumpForce * darkJumpMultiplier;
        damage = baseDamage + darkDamageBonus;
    }

    private void ApplyBaseFormNow()
    {
        isDarkForm = false;

        // Swap animator controller (dark -> base)
        if (animator != null && baseController != null)
            animator.runtimeAnimatorController = baseController;

        // Restore stats
        speed = baseSpeed;
        jumpForce = baseJumpForce;
        damage = baseDamage;
    }

    private void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // Trigger-based attack
        if (animator != null) animator.SetTrigger(AttackTrig);

        // Damage timing:
        if (attackHitDelay <= 0f)
        {
            PerformAttackHit();
        }
        else
        {
            StartCoroutine(PerformAttackHitDelayed(attackHitDelay));
        }
    }

    private IEnumerator PerformAttackHitDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        PerformAttackHit();
    }

    public void PerformAttackHit()
    {
        if (attackPoint == null) return;

        float radius = isDarkForm ? darkAttackRadius : baseAttackRadius;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, radius, enemyLayer);
        for (int i = 0; i < enemies.Length; i++)
        {
            Health enemyHealth = enemies[i].GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-damage);
            }
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    private void UpdateAnimatorParams()
    {
        if (animator == null || rb == null) return;

        float xVel = Mathf.Abs(rb.linearVelocity.x);
        float yVel = rb.linearVelocity.y;

        if (Mathf.Abs(yVel) < 0.05f) yVel = 0f;
        if (isGrounded && yVel < 0.05f) yVel = 0f;

        animator.SetFloat(XVel, xVel);
        animator.SetFloat(YVel, yVel);
        animator.SetBool(Grounded, isGrounded);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            TakeDamage(25);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            StartCoroutine(BlinkRed());
        }
    }

    private IEnumerator BlinkRed()
    {
        if (spriteRenderer != null) spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        RespawnState.IsRespawning = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void PlaySFX(AudioClip audioClip, float volume = 1f, float pitch = 1.5f)
    {
        if (audioSource == null || audioClip == null) return;

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();

        StartCoroutine(BlinkRed());
        if (health <= 0) Die();
    }

    private void UpdateHealthUI()
    {
        if (healthImage != null)
            healthImage.fillAmount = Mathf.Clamp01(health / (float)maxHealth);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            float radius = isDarkForm ? darkAttackRadius : baseAttackRadius;
            Gizmos.DrawWireSphere(attackPoint.position, radius);
        }


        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
