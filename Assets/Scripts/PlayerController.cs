using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IDamageable, IRechargeable
{
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 8f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float healthRegenRate = 2f; // 2 HP per second
    [SerializeField] private float healthRegenDelay = 3f; // 3 seconds before regen starts
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private float invulnerabilityDuration = 1f;
    
    [Header("Battery Settings")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float currentBattery = 100f;
    [SerializeField] private float batteryDrainRate = 8f; // Per second when flashlight on
    [SerializeField] private float batteryRechargeRate = 15f; // Per second when flashlight off
    
    [Header("References")]
    [SerializeField] private Transform flashlightPivot;
    [SerializeField] private Camera mainCamera;
    public Flashlight flashlight;
    public SpriteRenderer playerSprite;
    [SerializeField] private Animator playerAnimator;
    
    [Header("Flashlight Settings")]
    [SerializeField] private float flashlightRevealDamage = 10f; // Damage per second to enemies in light
    [SerializeField] private float flashlightDamageInterval = 0.2f; // How often to damage enemies
    
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem damageParticles;
    [SerializeField] private ParticleSystem healParticles;
    [SerializeField] private ParticleSystem batteryParticles;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private Color batteryLowColor = Color.yellow;
    [SerializeField] private Color regenColor = Color.green;
    [SerializeField] private float flashDuration = 0.1f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip batteryLowSound;
    [SerializeField] private AudioClip flashlightToggleSound;
    [SerializeField] private AudioClip deathSound;
    
    // Components
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Vector2 movementInput;
    private Vector2 currentVelocity;
    private Color originalColor;
    private float flashTimer = 0f;
    private float invulnerabilityTimer = 0f;
    private float flashlightDamageTimer = 0f;
    
    // Health Regen
    private float damageTakenTimer = 0f; // Timer for regeneration delay
    private bool isRegenerating = false;
    
    // Battery
    private float batteryRechargeDelay = 1f; // Delay before recharging starts
    private float batteryRechargeTimer = 0f;
    private bool isFlashlightOn = true;
    private bool isBatteryLow = false;
    private bool isBatteryDead = false;
    
    // Movement lock
    private bool isMovementLocked = false;
    private bool isStunned = false;
    private float stunTimer = 0f;
    
    // Events
    public event Action<float> OnDamageTaken;
    public event Action<float> OnHealed;
    public event Action OnDeath;
    public event Action<float> OnChargeChanged;
    public event Action<float> OnChargeConsumed;
    public event Action OnFullyCharged;
    public event Action OnBatteryEmpty;
    public event Action OnBatteryLow;
    public event Action OnBatteryRestored;
    public event Action<bool> OnFlashlightToggled;
    public event Action<bool> OnRegenerationStateChanged;
    
    // Interface Properties
    public float CurrentHealth { get => currentHealth; private set => currentHealth = Mathf.Clamp(value, 0, maxHealth); }
    public float MaxHealth { get => maxHealth; private set => maxHealth = Mathf.Max(0, value); }
    public bool IsDead { get => currentHealth <= 0; }
    
    public float CurrentCharge { get => currentBattery; private set => currentBattery = Mathf.Clamp(value, 0, maxBattery); }
    public float MaxCharge { get => maxBattery; private set => maxBattery = Mathf.Max(0, value); }
    public bool IsFullyCharged { get => currentBattery >= maxBattery; }
    
    // Public Properties
    public bool IsMovementLocked => isMovementLocked || isStunned || IsDead;
    public bool IsFlashlightOn => isFlashlightOn;
    public float BatteryPercentage => currentBattery / maxBattery;
    public float HealthPercentage => currentHealth / maxHealth;
    public Vector2 MovementInput => movementInput;
    public bool IsMoving => movementInput.magnitude > 0.1f && !IsMovementLocked;
    public Rigidbody2D Rigidbody => rb;
    public float FlashlightRevealDamage => flashlightRevealDamage;
    public bool IsRegenerating => isRegenerating;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        InitializeComponents();
    }
    
    void Start()
    {
        // Initialize stats
        currentHealth = maxHealth;
        currentBattery = maxBattery;
        originalColor = playerSprite != null ? playerSprite.color : Color.white;
        
        // Setup flashlight reference if not assigned
        if (flashlight == null && flashlightPivot != null)
        {
            flashlight = flashlightPivot.GetComponentInChildren<Flashlight>();
        }
        
        // Initialize flashlight
        if (flashlight != null)
        {
            flashlight.SetOwner(this);
            flashlight.SetActive(isFlashlightOn);
        }
        
        // Start with flashlight on
        TurnFlashlightOn();
    }
    
    void InitializeComponents()
    {
        if (flashlightPivot == null)
        {
            flashlightPivot = transform.Find("FlashlightPivot")?.transform;
            if (flashlightPivot == null)
            {
                GameObject pivotObj = new GameObject("FlashlightPivot");
                flashlightPivot = pivotObj.transform;
                flashlightPivot.SetParent(transform);
                flashlightPivot.localPosition = new Vector3(0.3f, 0, 0); // Slight offset
            }
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
    
    void Update()
    {
        if (IsDead) return;
        
        HandleInput();
        UpdateTimers();
        UpdateHealthRegeneration();
        HandleBattery();
        HandleFlash();
        RotateFlashlight();
        UpdateAnimations();
        
        // Apply flashlight damage to enemies
        if (isFlashlightOn && flashlight != null && flashlight.IsActive)
        {
            ApplyFlashlightDamage();
        }
    }
    
    void FixedUpdate()
    {
        if (!IsMovementLocked)
        {
            MovePlayer();
        }
        else
        {
            // Stop movement when locked
            if (rb != null)
                rb.velocity = Vector2.zero;
        }
    }
    
    void HandleInput()
    {
        // Movement input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        movementInput = new Vector2(horizontal, vertical);
        
        if (movementInput.magnitude > 1f)
            movementInput.Normalize();
        
        // Toggle flashlight
        if (Input.GetKeyDown(KeyCode.F) && !isMovementLocked)
        {
            ToggleFlashlight();
        }
        
        // Debug/testing keys
        if (Input.GetKeyDown(KeyCode.R))
        {
            Recharge(20f);
        }
    }
    
    void UpdateTimers()
    {
        if (flashTimer > 0) flashTimer -= Time.deltaTime;
        if (invulnerabilityTimer > 0) invulnerabilityTimer -= Time.deltaTime;
        if (stunTimer > 0) stunTimer -= Time.deltaTime;
        if (batteryRechargeTimer > 0) batteryRechargeTimer -= Time.deltaTime;
        if (flashlightDamageTimer > 0) flashlightDamageTimer -= Time.deltaTime;
        if (damageTakenTimer > 0) damageTakenTimer -= Time.deltaTime;
        
        // Update invulnerability state
        isInvulnerable = invulnerabilityTimer > 0;
        
        // Update stun state
        if (stunTimer <= 0) isStunned = false;
    }
    
    void UpdateHealthRegeneration()
    {
        // Check if we should start regenerating
        if (!isRegenerating && damageTakenTimer <= 0 && currentHealth < maxHealth && !IsDead)
        {
            StartRegeneration();
        }
        
        // Apply regeneration if active
        if (isRegenerating)
        {
            float healAmount = healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
            
            // Trigger heal event for UI updates
            OnHealed?.Invoke(healAmount);
            
            // Stop if fully healed
            if (currentHealth >= maxHealth)
            {
                StopRegeneration();
            }
        }
    }
    
    void StartRegeneration()
    {
        isRegenerating = true;
        OnRegenerationStateChanged?.Invoke(true);
    }
    
    void StopRegeneration()
    {
        isRegenerating = false;
        OnRegenerationStateChanged?.Invoke(false);
    }
    
    void ResetRegenerationTimer()
    {
        damageTakenTimer = healthRegenDelay; // 3 seconds
        if (isRegenerating)
        {
            StopRegeneration();
        }
    }
    
    void MovePlayer()
    {
        if (rb == null) return;
        
        Vector2 targetVelocity = movementInput * moveSpeed;
        float currentAcceleration = movementInput.magnitude > 0.1f ? acceleration : deceleration;
        
        currentVelocity = Vector2.Lerp(
            currentVelocity,
            targetVelocity,
            currentAcceleration * Time.fixedDeltaTime
        );
        
        rb.velocity = currentVelocity;
        
        // Flip sprite based on movement
        if (playerSprite != null && Mathf.Abs(movementInput.x) > 0.1f)
        {
            playerSprite.flipX = movementInput.x < 0;
        }
    }
    
    void RotateFlashlight()
    {
        if (flashlightPivot == null || mainCamera == null || IsMovementLocked) return;
        
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        Vector3 direction = mousePos - flashlightPivot.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        flashlightPivot.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    void HandleBattery()
    {
        if (IsDead) return;
        
        // Drain battery if flashlight is on and active
        if (isFlashlightOn && flashlight != null && flashlight.IsActive && currentBattery > 0)
        {
            float drainAmount = batteryDrainRate * Time.deltaTime;
            ConsumeCharge(drainAmount);
            batteryRechargeTimer = batteryRechargeDelay;
            isBatteryDead = false;
        }
        else if (!isFlashlightOn && !IsFullyCharged && batteryRechargeTimer <= 0)
        {
            // Recharge when flashlight is off
            float rechargeAmount = batteryRechargeRate * Time.deltaTime;
            Recharge(rechargeAmount);
        }
        
        // Auto-turn off flashlight if battery dead
        if (currentBattery <= 0 && isFlashlightOn && !isBatteryDead)
        {
            isBatteryDead = true;
            TurnFlashlightOff();
            OnBatteryEmpty?.Invoke();
        }
        
        // Check for battery warnings
        float batteryPercent = BatteryPercentage;
        if (batteryPercent <= 0.2f && !isBatteryLow && currentBattery > 0)
        {
            isBatteryLow = true;
            OnBatteryLow?.Invoke();
            
            if (audioSource != null && batteryLowSound != null)
                audioSource.PlayOneShot(batteryLowSound, 0.5f);
        }
        else if (batteryPercent > 0.2f && isBatteryLow)
        {
            isBatteryLow = false;
            OnBatteryRestored?.Invoke();
        }
    }
    
    void HandleFlash()
    {
        if (playerSprite == null) return;
        
        if (flashTimer > 0)
        {
            float t = flashTimer / flashDuration;
            playerSprite.color = Color.Lerp(originalColor, damageFlashColor, t);
        }
        else if (isRegenerating)
        {
            // Pulse green when regenerating
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            playerSprite.color = Color.Lerp(originalColor, regenColor, pulse * 0.3f);
        }
        else if (isBatteryLow && isFlashlightOn)
        {
            // Pulse yellow when battery is low
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            playerSprite.color = Color.Lerp(originalColor, batteryLowColor, pulse * 0.3f);
        }
        else
        {
            playerSprite.color = originalColor;
        }
    }
    
    void UpdateAnimations()
    {
        if (playerAnimator == null) return;
        
        playerAnimator.SetBool("IsMoving", IsMoving);
        playerAnimator.SetFloat("MoveSpeed", currentVelocity.magnitude / moveSpeed);
        playerAnimator.SetBool("IsDead", IsDead);
        playerAnimator.SetBool("IsStunned", isStunned);
        playerAnimator.SetBool("FlashlightOn", isFlashlightOn);
        playerAnimator.SetBool("IsRegenerating", isRegenerating);
        
        if (IsMoving)
        {
            playerAnimator.SetFloat("MoveX", movementInput.x);
            playerAnimator.SetFloat("MoveY", movementInput.y);
        }
    }
    
    void ApplyFlashlightDamage()
    {
        if (flashlightDamageTimer > 0) return;
        
        if (flashlight != null)
        {
            // Let the flashlight handle enemy damage
            flashlight.DamageEnemiesInLight();
        }
        
        flashlightDamageTimer = flashlightDamageInterval;
    }
    
    #region Flashlight Control
    
    public void ToggleFlashlight()
    {
        if (flashlight == null || isBatteryDead) return;
        
        if (isFlashlightOn)
        {
            TurnFlashlightOff();
        }
        else
        {
            TurnFlashlightOn();
        }
        
        // Play sound
        if (audioSource != null && flashlightToggleSound != null)
            audioSource.PlayOneShot(flashlightToggleSound, 0.5f);
        
        OnFlashlightToggled?.Invoke(isFlashlightOn);
    }
    
    public void TurnFlashlightOn()
    {
        if (flashlight == null || currentBattery <= 0) return;
        
        isFlashlightOn = true;
        if (flashlight != null)
            flashlight.SetActive(true);
    }
    
    public void TurnFlashlightOff()
    {
        if (flashlight == null) return;
        
        isFlashlightOn = false;
        if (flashlight != null)
            flashlight.SetActive(false);
        
        // Start recharge timer
        batteryRechargeTimer = batteryRechargeDelay;
    }
    
    public void SetFlashlightActive(bool active)
    {
        if (active) TurnFlashlightOn();
        else TurnFlashlightOff();
    }
    
    #endregion
    
    #region IDamageable Implementation
    
    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0 || isInvulnerable) return;
        
        float oldHealth = currentHealth;
        CurrentHealth -= damage;
        float actualDamage = oldHealth - currentHealth;
        
        // Visual feedback
        flashTimer = flashDuration;
        if (playerSprite != null)
            playerSprite.color = damageFlashColor;
        
        if (damageParticles != null)
            damageParticles.Play();
        
        // Audio feedback
        if (audioSource != null && damageSound != null)
            audioSource.PlayOneShot(damageSound, 0.7f);
        
        // Reset regeneration timer when taking damage
        ResetRegenerationTimer();
        
        OnDamageTaken?.Invoke(actualDamage);
        
        // Apply invulnerability
        invulnerabilityTimer = invulnerabilityDuration;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0) return;
        
        float oldHealth = currentHealth;
        CurrentHealth += amount;
        float actualHeal = currentHealth - oldHealth;
        
        if (healParticles != null)
            healParticles.Play();
        
        if (audioSource != null && healSound != null)
            audioSource.PlayOneShot(healSound, 0.5f);
        
        OnHealed?.Invoke(actualHeal);
    }
    
    private void Die()
    {
        // Disable movement
        isMovementLocked = true;
        if (rb != null)
            rb.velocity = Vector2.zero;
        
        // Visual feedback
        if (playerSprite != null)
            playerSprite.color = Color.gray;
        
        // Disable flashlight
        TurnFlashlightOff();
        
        // Stop regeneration
        StopRegeneration();
        
        // Play death sound
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound, 1f);
        
        OnDeath?.Invoke();
        
        Debug.Log("Player died!");
        // Trigger game over
    }
    
    #endregion
    
    #region IRechargeable Implementation
    
    public void Recharge(float amount)
    {
        if (amount <= 0 || IsFullyCharged) return;
        
        float oldBattery = currentBattery;
        CurrentCharge += amount;
        float actualRecharge = currentBattery - oldBattery;
        
        if (batteryParticles != null && actualRecharge > 0)
            batteryParticles.Play();
        
        // Audio feedback for large recharges
        if (actualRecharge > 10f && audioSource != null && batteryLowSound != null)
            audioSource.PlayOneShot(batteryLowSound, 0.3f);
        
        OnChargeChanged?.Invoke(currentBattery);
        
        if (IsFullyCharged)
        {
            OnFullyCharged?.Invoke();
        }
        
        // Battery is no longer dead if we recharged
        if (currentBattery > 0 && isBatteryDead)
        {
            isBatteryDead = false;
        }
    }
    
    public void ConsumeCharge(float amount)
    {
        if (amount <= 0) return;
        
        float oldBattery = currentBattery;
        CurrentCharge -= amount;
        float actualConsumed = Mathf.Max(0, oldBattery - currentBattery);
        
        OnChargeChanged?.Invoke(currentBattery);
        OnChargeConsumed?.Invoke(actualConsumed);
    }
    
    public void SetMaxCharge(float newMax)
    {
        if (newMax <= 0) return;
        
        float percentage = currentBattery / maxBattery;
        MaxCharge = newMax;
        CurrentCharge = maxBattery * percentage;
        
        OnChargeChanged?.Invoke(currentBattery);
    }
    
    #endregion
    
    #region Public Methods
    
    public void LockMovement() => isMovementLocked = true;
    public void UnlockMovement() => isMovementLocked = false;
    public void SetMovementLock(bool locked) => isMovementLocked = locked;
    
    public void Stun(float duration)
    {
        if (IsDead) return;
        
        isStunned = true;
        stunTimer = duration;
        
        // Stop movement
        if (rb != null)
            rb.velocity = Vector2.zero;
    }
    
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (rb == null || IsDead) return;
        
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }
    
    public void SetMoveSpeed(float newSpeed) => moveSpeed = Mathf.Max(0, newSpeed);
    public void ResetMoveSpeed() => moveSpeed = baseMoveSpeed;
    public void MultiplyMoveSpeed(float multiplier) => moveSpeed = Mathf.Max(0, moveSpeed * multiplier);
    
    public void IncreaseMaxHealth(float amount)
    {
        MaxHealth += amount;
        CurrentHealth += amount;
    }
    
    public void IncreaseMaxBattery(float amount) => SetMaxCharge(maxBattery + amount);
    
    public void SetInvulnerable(bool invulnerable, float duration = 0f)
    {
        isInvulnerable = invulnerable;
        if (duration > 0)
            invulnerabilityTimer = duration;
    }
    
    public void Teleport(Vector2 position)
    {
        if (rb == null) return;
        
        rb.position = position;
        rb.velocity = Vector2.zero;
        currentVelocity = Vector2.zero;
    }
    
    #endregion
    
    #region Collision Handling
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Handle enemy collisions
        if (collision.gameObject.CompareTag("Enemy"))
        {
            BaseEnemy enemy = collision.gameObject.GetComponent<BaseEnemy>();
            if (enemy != null && enemy.IsRevealed)
            {
                // Take damage based on enemy contact
                TakeDamage(enemy.damageOnContact);
                
                // Apply knockback
                Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
                ApplyKnockback(knockbackDirection, 5f);
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle battery pickups
        if (other.CompareTag("BatteryPickup"))
        {
            Recharge(50f);
            Destroy(other.gameObject);
        }
        // No health pickups - player only regenerates naturally
    }
    
    #endregion
    
    #region Debug GUI
    
    void OnGUI()
    {
        // Debug display
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Health: {currentHealth:F0}/{maxHealth:F0}");
        GUILayout.Label($"Battery: {currentBattery:F0}/{maxBattery:F0}");
        GUILayout.Label($"Flashlight: {(isFlashlightOn ? "ON" : "OFF")}");
        GUILayout.Label($"Regenerating: {isRegenerating}");
        GUILayout.Label($"Regen Timer: {damageTakenTimer:F1}s");
        GUILayout.Label($"Invulnerable: {isInvulnerable} ({invulnerabilityTimer:F1})");
        GUILayout.Label($"Controls: F-Toggle Flashlight");
        GUILayout.EndArea();
    }
    
    #endregion
}