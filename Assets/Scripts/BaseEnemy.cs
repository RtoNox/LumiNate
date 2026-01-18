using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseEnemy : MonoBehaviour, IDamageable, IRevealable, IWaveEntity
{
    [Header("Base Enemy Settings")]
    [SerializeField] protected float maxHealth = 50f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float chaseSpeed = 5f;
    public float damageOnContact = 10f;
    [SerializeField] protected float attackCooldown = 1f;
    
    [Header("Reveal Settings")]
    [SerializeField] protected float baseRevealDuration = 3f;
    [SerializeField] protected Color hiddenColor = Color.black;
    [SerializeField] protected Color revealedColor = Color.red;
    [SerializeField] protected Color flashlightHitColor = Color.white;
    
    [Header("Detection & Chase")]
    [SerializeField] protected float detectionRange = 15f;
    [SerializeField] protected float chaseRange = 20f;
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] protected LayerMask obstacleLayer;
    
    [Header("Flashlight Interaction")]
    [SerializeField] protected float flashlightDamageMultiplier = 1f;
    [SerializeField] protected float flashlightHitDuration = 0.2f;
    
    // Components
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected PlayerController playerController;
    protected Animator animator;
    
    // State
    protected float currentHealth;
    protected bool isRevealed = false;
    protected float revealTimer = 0f;
    protected bool isActive = true;
    protected bool isCompleted = false;
    protected bool canAttack = true;
    protected Color currentColor;
    protected float flashlightHitTimer = 0f;
    protected bool isChasing = false;
    
    // Movement
    protected Vector2 movementDirection;
    protected bool isMoving = false;
    protected Vector2 wanderDirection;
    protected float wanderTimer = 0f;
    protected float wanderChangeTime = 2f;
    
    // Events
    public event System.Action<float> OnDamageTaken;
    public event System.Action<float> OnHealed;
    public event System.Action OnDeath;
    public event System.Action OnRevealed;
    public event System.Action OnHidden;
    public event System.Action<int> OnWaveStarted;
    public event System.Action<int> OnWaveCompleted;
    public event System.Action<int> OnWaveFailed;
    
    // Interface Properties
    public float CurrentHealth { get => currentHealth; protected set => currentHealth = Mathf.Clamp(value, 0, maxHealth); }
    public float MaxHealth { get => maxHealth; protected set => maxHealth = Mathf.Max(0, value); }
    public bool IsDead { get => currentHealth <= 0; }
    
    public bool IsRevealed => isRevealed;
    public float RevealDuration => baseRevealDuration;
    public float RevealTimer => revealTimer;
    
    public int WaveNumber => waveNumber;
    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    
    // Public properties
    protected int waveNumber = 1;
    
    // Properties for child classes
    protected Transform PlayerTransform => playerTransform;
    protected PlayerController PlayerController => playerController;
    protected bool IsPlayerInRange => playerTransform != null && 
        Vector2.Distance(transform.position, playerTransform.position) <= detectionRange;
    protected bool IsPlayerInChaseRange => playerTransform != null && 
        Vector2.Distance(transform.position, playerTransform.position) <= chaseRange;
    protected bool HasLineOfSightToPlayer
    {
        get
        {
            if (playerTransform == null) return false;
            
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                direction,
                distance,
                obstacleLayer
            );
            
            return hit.collider == null || hit.collider.CompareTag("Player");
        }
    }
    
    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
        
        currentColor = hiddenColor;
        currentHealth = maxHealth;
        
        FindPlayer();
    }
    
    protected virtual void Start()
    {
        if (animator != null)
        {
            animator.SetBool("IsActive", isActive);
        }
        
        // Start wandering
        SetRandomWanderDirection();
    }
    
    protected virtual void Update()
    {
        if (IsDead || !isActive) return;
        
        UpdateReveal();
        UpdateColor();
        UpdateFlashlightHit();
        UpdateWander();
        
        // Check if player is in range and has line of sight
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= chaseRange && HasLineOfSightToPlayer)
            {
                isChasing = true;
                ChasePlayer();
            }
            else
            {
                isChasing = false;
                Wander();
            }
        }
        else
        {
            // If player is null, try to find them
            FindPlayer();
            Wander();
        }
        
        // Update movement based on state
        if (isChasing)
        {
            ChaseBehavior();
        }
        else if (isRevealed)
        {
            RevealedBehavior();
        }
        else
        {
            HiddenBehavior();
        }
    }
    
    protected virtual void FixedUpdate()
    {
        if (IsDead || !isActive || !isMoving) return;
        
        ApplyMovement();
    }
    
    #region Core Methods
    
    protected virtual void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure Player GameObject has 'Player' tag.");
        }
    }
    
    protected virtual void UpdateReveal()
    {
        if (isRevealed)
        {
            revealTimer -= Time.deltaTime;
            if (revealTimer <= 0)
            {
                Hide();
            }
        }
    }
    
    protected virtual void UpdateColor()
    {
        if (flashlightHitTimer > 0)
        {
            // Flash white when hit by flashlight
            float t = flashlightHitTimer / flashlightHitDuration;
            spriteRenderer.color = Color.Lerp(currentColor, flashlightHitColor, t);
        }
        else if (isRevealed)
        {
            float t = revealTimer / baseRevealDuration;
            spriteRenderer.color = Color.Lerp(hiddenColor, revealedColor, t);
            currentColor = spriteRenderer.color;
        }
        else
        {
            spriteRenderer.color = hiddenColor;
            currentColor = hiddenColor;
        }
    }
    
    protected virtual void UpdateFlashlightHit()
    {
        if (flashlightHitTimer > 0)
        {
            flashlightHitTimer -= Time.deltaTime;
        }
    }
    
    protected virtual void UpdateWander()
    {
        if (wanderTimer > 0)
        {
            wanderTimer -= Time.deltaTime;
        }
        else
        {
            SetRandomWanderDirection();
        }
    }
    
    protected virtual void SetRandomWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        wanderTimer = Random.Range(1f, wanderChangeTime);
    }
    
    protected virtual void Wander()
    {
        if (!isChasing && !isRevealed)
        {
            movementDirection = wanderDirection;
            isMoving = true;
        }
    }
    
    protected virtual void ChasePlayer()
    {
        if (playerTransform == null) return;
        
        movementDirection = (playerTransform.position - transform.position).normalized;
        isMoving = true;
    }
    
    protected virtual void ApplyMovement()
    {
        if (rb == null || !isMoving) return;
        
        float currentSpeed = isChasing ? chaseSpeed : moveSpeed;
        
        // Apply movement
        if (rb != null)
        {
            rb.velocity = movementDirection * currentSpeed;
        }
        else
        {
            // Fallback if no rigidbody
            transform.position += (Vector3)movementDirection * currentSpeed * Time.fixedDeltaTime;
        }
        
        // Flip sprite based on movement direction
        if (spriteRenderer != null && Mathf.Abs(movementDirection.x) > 0.1f)
        {
            spriteRenderer.flipX = movementDirection.x < 0;
        }
    }
    
    #endregion
    
    #region Abstract Methods (Must be implemented by child classes)
    
    protected abstract void ChaseBehavior();
    protected abstract void RevealedBehavior();
    protected abstract void HiddenBehavior();
    protected abstract void OnPlayerContact(GameObject player);
    
    #endregion
    
    #region Virtual Methods (Can be overridden by child classes)
    
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerContact(collision.gameObject);
        }
    }
    
    protected virtual IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
    
    protected virtual void DealDamageToPlayer(float damage)
    {
        if (playerController == null) return;
        playerController.TakeDamage(damage);
    }
    
    protected virtual void HealPlayer(float amount)
    {
        if (playerController == null) return;
        playerController.Heal(amount);
    }
    
    protected virtual void DrainPlayerBattery(float amount)
    {
        if (playerController == null) return;
        playerController.ConsumeCharge(amount);
    }
    
    protected virtual void RechargePlayerBattery(float amount)
    {
        if (playerController == null) return;
        playerController.Recharge(amount);
    }
    
    #endregion
    
    #region IDamageable Implementation
    
    public virtual void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0) return;
        
        float oldHealth = currentHealth;
        CurrentHealth -= damage;
        float actualDamage = oldHealth - currentHealth;
        
        // Trigger reveal if not already revealed
        if (!isRevealed)
        {
            Reveal();
        }
        
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
        
        OnDamageTaken?.Invoke(actualDamage);
        
        if (IsDead)
        {
            Die();
        }
    }
    
    public virtual void Heal(float amount)
    {
        if (IsDead || amount <= 0) return;
        
        float oldHealth = currentHealth;
        CurrentHealth += amount;
        float actualHeal = currentHealth - oldHealth;
        
        if (animator != null)
        {
            animator.SetTrigger("Heal");
        }
        
        OnHealed?.Invoke(actualHeal);
    }
    
    // Called when hit by flashlight
    public virtual void FlashlightHit(Vector3 sourcePosition)
    {
        if (IsDead) return;
        
        flashlightHitTimer = flashlightHitDuration;
        
        // Take extra damage from flashlight
        if (playerController != null)
        {
            float damage = playerController.FlashlightRevealDamage * flashlightDamageMultiplier * Time.deltaTime;
            TakeDamage(damage);
        }
        
        // Reveal if not already revealed
        if (!isRevealed)
        {
            Reveal();
        }
    }
    
    protected virtual void Die()
    {
        isActive = false;
        isMoving = false;
        
        // Disable physics
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Visual feedback
        spriteRenderer.color = Color.gray;
        
        // Play death animation
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Die");
        }
        
        // Disable collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;
        
        OnDeath?.Invoke();
        
        // Mark wave as completed
        if (isActive && !isCompleted)
        {
            CompleteWave();
        }
        
        // Destroy after delay
        Destroy(gameObject, 2f);
    }
    
    #endregion
    
    #region IRevealable Implementation
    
    public virtual void Reveal()
    {
        if (IsDead || isRevealed) return;
        
        isRevealed = true;
        revealTimer = baseRevealDuration;
        
        if (animator != null)
        {
            animator.SetBool("IsRevealed", true);
        }
        
        OnRevealed?.Invoke();
    }
    
    public virtual void Hide()
    {
        if (!isRevealed) return;
        
        isRevealed = false;
        revealTimer = 0f;
        
        if (animator != null)
        {
            animator.SetBool("IsRevealed", false);
        }
        
        OnHidden?.Invoke();
    }
    
    public virtual void SetRevealDuration(float duration)
    {
        baseRevealDuration = Mathf.Max(0.1f, duration);
    }
    
    #endregion
    
    #region IWaveEntity Implementation
    
    public virtual void InitializeWave(int waveNum)
    {
        waveNumber = waveNum;
        isActive = true; // Ensure enemy is active
        isCompleted = false;
        
        // Scale stats based on wave number
        float waveMultiplier = 1 + (waveNumber * 0.1f);
        maxHealth *= waveMultiplier;
        currentHealth = maxHealth;
        moveSpeed *= (1 + (waveNumber * 0.05f));
        chaseSpeed *= (1 + (waveNumber * 0.05f));
        damageOnContact *= waveMultiplier;
    }
    
    public virtual void StartWave()
    {
        isActive = true;
        isCompleted = false;
        
        if (animator != null)
        {
            animator.SetBool("IsActive", true);
        }
        
        OnWaveStarted?.Invoke(waveNumber);
    }
    
    public virtual void CompleteWave()
    {
        if (!isActive || isCompleted) return;
        
        isCompleted = true;
        isActive = false;
        
        OnWaveCompleted?.Invoke(waveNumber);
    }
    
    public virtual void FailWave()
    {
        if (!isActive || isCompleted) return;
        
        isCompleted = false;
        isActive = false;
        
        if (animator != null)
        {
            animator.SetBool("IsActive", false);
        }
        
        OnWaveFailed?.Invoke(waveNumber);
    }
    
    #endregion
    
    #region Public Methods
    
    public virtual void SetMovementSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0, speed);
    }
    
    public virtual void SetChaseSpeed(float speed)
    {
        chaseSpeed = Mathf.Max(0, speed);
    }
    
    public virtual void SetMaxHealth(float health)
    {
        float percentage = currentHealth / maxHealth;
        MaxHealth = Mathf.Max(1, health);
        CurrentHealth = maxHealth * percentage;
    }
    
    public virtual void IncreaseStats(float healthMultiplier, float speedMultiplier, float damageMultiplier)
    {
        maxHealth *= healthMultiplier;
        currentHealth = maxHealth;
        moveSpeed *= speedMultiplier;
        chaseSpeed *= speedMultiplier;
        damageOnContact *= damageMultiplier;
    }
    
    #endregion
    
    #region Gizmos
    
    protected virtual void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw chase range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Draw line to player if in range
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance <= chaseRange)
            {
                bool hasLOS = HasLineOfSightToPlayer;
                Gizmos.color = hasLOS ? Color.green : Color.yellow;
                Gizmos.DrawLine(transform.position, playerTransform.position);
                
                // Draw LOS status
                Gizmos.color = hasLOS ? Color.green : Color.red;
                Gizmos.DrawSphere(transform.position, 0.3f);
            }
        }
        
        // Draw movement direction
        if (Application.isPlaying && isMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, movementDirection * 2f);
        }
    }
    
    #endregion
}