using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/*
2/5 - added the variable jump and got rid of wall and floor sticking. 
 next task will be to make more platforming, find a proper dash animation
 and start the npc dialogue and interaction. 
 
side quests
 - create secret rooms usings pixel art I found
 - hollow knight camera system
*/

public class Player : MonoBehaviour
{
    #region COMPONENTS
    [Header("Component References")]
    public Rigidbody2D rb;
    public PlayerInput playerInput;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;
    [SerializeField] private TrailRenderer tr;
    #endregion

    #region PLAYER STATS
    [Header("Player Stats")]
    public int health = 100;
    public int coins;
    public int damage = 10;
    public float baseScale = 4f;
    public int facingDirection = 1;
    #endregion

    #region MOVEMENT SETTINGS
    [Header("Movement Settings")]
    public float speed = 5f;
    public Vector2 moveInput;
    #endregion

    #region JUMP SETTINGS
    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float jumpCutMultiplier = 1f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public int extraJumpsValue = 1;
    private bool isGrounded;
    private int extraJumps;
    #endregion

    #region DASH SETTINGS
    [Header("Dash Settings")]
    public float dashPower = 24f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;
    #endregion

    #region UI REFERENCES
    [Header("UI References")]
    public Image healthImage;
    #endregion

    #region ATTACK REFERENCES
    [Header("Attack References")]
    public Transform attackPoint;
    public float attackRadius = 0.5f;
    public LayerMask enemyLayer;
   // public PlayerAttackState attackState;

    
    #endregion

    #region PRIVATE STATE VARIABLES
    private bool isAttacking = false;
    private float attackCooldown = 0.8f;
    private float lastAttackTime = 0f;
    public bool attackPressed;
    #endregion

    #region UNITY LIFE CYCLE METHODS
    void Start()
    {
        InitializeComponents();
        InitializeState();
    }

    void Update()
    {
        if (isDashing) return;
        
        UpdateGroundCheck();
        UpdateAnimations();
        UpdateHealthUI();
        HandleJumpCut();
    }
    
    void FixedUpdate()
    {
        if (isDashing) return;
        
        if (!isAttacking)
        {
            MovePlayer();
        }
        else
        {
            StopMovementDuringAttack();
        }
    }
    #endregion

    #region INITIALIZATION
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    private void InitializeState()
    {
        extraJumps = extraJumpsValue;
    }
    #endregion

    #region MOVEMENT
    private void MovePlayer()
    {
        float targetSpeed = moveInput.x * speed;
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    private void StopMovementDuringAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        Debug.Log("Move input: " + moveInput);
    }

    private void Flip()
    {
        if (moveInput.x > 0.1f)
        {
            facingDirection = 1;
        }
        else if (moveInput.x < -0.1f)
        {
            facingDirection = -1;
        }
        
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            transform.localScale = new Vector3(baseScale * facingDirection, baseScale, baseScale);
        }
    }
    #endregion

    #region JUMP
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Jump button pressed");
            if (!isAttacking && !isDashing)
            {
                if (isGrounded)
                {
                    PerformJump();
                    Debug.Log("Jumped - grounded");
                }
                else if (extraJumps > 0)
                {
                    PerformJump();
                    extraJumps--;
                    Debug.Log("Jumped - extra jump. Remaining: " + extraJumps);
                }
            }
            else
            {
                Debug.Log("Jump blocked - currently attacking or dashing");
            }
        }
    }

    private void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void HandleJumpCut()
    {
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void UpdateGroundCheck()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        if (isGrounded)
        {
            extraJumps = extraJumpsValue;
            canDash = true;
        }
    }
    #endregion

    #region DASH
    public void OnDash(InputValue value)
    {
        Debug.Log("Dash button pressed - isPressed: " + value.isPressed);
        
        if (value.isPressed && canDash && !isAttacking)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        Debug.Log("Starting dash!");
        StartCoroutine(Dash());
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
        
        // Visual effects
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
        
        Debug.Log("Dash ready again");
    }
    #endregion

    #region ANIMATION
    private void UpdateAnimations()
    {
        Flip();
        SetAnimation(moveInput.x);
    }

    private void SetAnimation(float moveInput)
    {
        if (animator == null) return;

        if (isDashing)
        {
            animator.Play("player_dash");
            return;
        }

        if (isAttacking)
        {
            return;
        }

        if (isGrounded)
        {
            if (moveInput == 0)
                animator.Play("player_idle");
            else
                animator.Play("player_run");
        }
        else
        {
            if (rb.linearVelocity.y > 0)
                animator.Play("player_jump");
            else
                animator.Play("player_fall");
        }
    }
    #endregion

    #region ATTACK
    public void OnAttack(InputValue value)
    {
        Debug.Log("Attack button pressed - isPressed: " + value.isPressed);
        
        if (value.isPressed && !isAttacking && Time.time >= lastAttackTime + attackCooldown && !isDashing)
        {
            StartAttack();
        }
        else
        {
            Debug.Log("Attack blocked - either already attacking, on cooldown, or dashing");
        }
    }
    private void StartAttack()
    {
    Debug.Log("Starting attack!");
    isAttacking = true;
    lastAttackTime = Time.time;
    animator.SetBool("isAttacking", true);
    
    // Turn on and play spellFX
    GameObject spellFX = transform.Find("spellFX")?.gameObject;
    if (spellFX != null)
    {
        spellFX.SetActive(true);
        spellFX.GetComponent<Animator>().Play("hitFX", 0, 0f);
    }
    
    Debug.Log("Attack animation triggered");
    PerformAttack();
    StartCoroutine(ResetAttack());
    }

    private void PerformAttack()
    {
        Collider2D enemy = Physics2D.OverlapCircle(attackPoint.position, attackRadius, enemyLayer);
        if (enemy != null)
        {
            Debug.Log("Enemy hit: " + enemy.gameObject.name);
            Health enemyHealth = enemy.gameObject.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-damage);
                Debug.Log("Enemy damaged: " + damage + " damage dealt");
            }
            else
            {
                Debug.Log("No Health component found on enemy");
            }
        }
        else
        {
            Debug.Log("No enemy in attack range");
        }
    }

    private IEnumerator ResetAttack()
    {
    yield return new WaitForSeconds(attackCooldown);
    isAttacking = false;
    animator.SetBool("isAttacking", false);
    
    // Turn off the spellFX object
    transform.Find("spellFX")?.gameObject.SetActive(false);
    
    Debug.Log("Attack finished - can attack again");
    }   
    #endregion

    #region HEALTH & DAMAGE
    private void UpdateHealthUI()
    {
        healthImage.fillAmount = health / 100f;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDashing)
            return;
            
        health -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage! Health: " + health);
        
        StartCoroutine(BlinkRed());
        
        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator BlinkRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f); 
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage") && !isDashing)
        {
            health -= 25;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            StartCoroutine(BlinkRed());

            if (health <= 0)
                Die();
        }
    }
    #endregion

    #region AUDIO
    public void PlaySFX(AudioClip audioClip, float volume = 1f, float pitch = 1.5f)
    {
        if (audioSource == null || audioClip == null) return;

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
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