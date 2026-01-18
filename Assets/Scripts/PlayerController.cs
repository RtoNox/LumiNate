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
    [SerializeField] private float healthRegenRate = 2f;
    [SerializeField] private float healthRegenDelay = 3f;
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private float invulnerabilityDuration = 1f;
    
    [Header("Battery Settings")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float currentBattery = 100f;
    [SerializeField] private float batteryDrainRate = 8f;
    [SerializeField] private float batteryRechargeRate = 15f;
    
    [Header("Win Condition")]
    [SerializeField] private int requiredWinItems = 1;
    private int collectedWinItems = 0;
    
    [Header("References")]
    [SerializeField] private Transform flashlightPivot;
    [SerializeField] private Camera mainCamera;
    public Flashlight flashlight;
    public SpriteRenderer playerSprite;
    [SerializeField] private Animator playerAnimator;
    
    [Header("Flashlight Settings")]
    [SerializeField] private float flashlightRevealDamage = 10f;
    
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
    [SerializeField] private AudioClip winItemPickupSound;
    
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Vector2 movementInput;
    private Vector2 currentVelocity;
    private Color originalColor;
    private float flashTimer = 0f;
    private float invulnerabilityTimer = 0f;
    private float flashlightDamageTimer = 0f;
    
    private float damageTakenTimer = 0f;
    private bool isRegenerating = false;
    
    private float batteryRechargeDelay = 1f;
    private float batteryRechargeTimer = 0f;
    private bool isFlashlightOn = false;
    private bool isBatteryLow = false;
    private bool isBatteryDead = false;
    
    private bool isMovementLocked = false;
    private bool isStunned = false;
    private float stunTimer = 0f;
    
    private bool hasWon = false;
    
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
    public event Action<int> OnWinItemCollected;
    public event Action OnWinConditionMet;
    
    public float CurrentHealth { get => currentHealth; private set => currentHealth = Mathf.Clamp(value, 0, maxHealth); }
    public float MaxHealth { get => maxHealth; private set => maxHealth = Mathf.Max(0, value); }
    public bool IsDead { get => currentHealth <= 0; }
    
    public float CurrentCharge { get => currentBattery; private set => currentBattery = Mathf.Clamp(value, 0, maxBattery); }
    public float MaxCharge { get => maxBattery; private set => maxBattery = Mathf.Max(0, value); }
    public bool IsFullyCharged { get => currentBattery >= maxBattery; }
    
    public bool IsMovementLocked => isMovementLocked || isStunned || IsDead || hasWon;
    public bool IsFlashlightOn => isFlashlightOn;
    public float BatteryPercentage => currentBattery / maxBattery;
    public float HealthPercentage => currentHealth / maxHealth;
    public Vector2 MovementInput => movementInput;
    public bool IsMoving => movementInput.magnitude > 0.1f && !IsMovementLocked;
    public Rigidbody2D Rigidbody => rb;
    public float FlashlightRevealDamage => flashlightRevealDamage;
    public bool IsRegenerating => isRegenerating;
    public int CollectedWinItems => collectedWinItems;
    public int RequiredWinItems => requiredWinItems;
    public bool HasWon => hasWon;
    
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
        currentHealth = maxHealth;
        currentBattery = maxBattery;
        originalColor = playerSprite != null ? playerSprite.color : Color.white;
        
        if (flashlight == null && flashlightPivot != null)
        {
            flashlight = flashlightPivot.GetComponentInChildren<Flashlight>();
        }
        
        if (flashlight != null)
        {
            flashlight.SetOwner(this);
            flashlight.SetActive(isFlashlightOn);
        }
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
                flashlightPivot.localPosition = new Vector3(0.3f, 0, 0);
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
        if (IsDead || hasWon) return;
        
        HandleInput();
        UpdateTimers();
        UpdateHealthRegeneration();
        HandleBattery();
        HandleFlash();
        RotateFlashlight();
        UpdateAnimations();
    }
    
    void FixedUpdate()
    {
        if (!IsMovementLocked)
        {
            MovePlayer();
        }
        else
        {
            if (rb != null)
                rb.velocity = Vector2.zero;
        }
    }
    
    void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        movementInput = new Vector2(horizontal, vertical);
        
        if (movementInput.magnitude > 1f)
            movementInput.Normalize();
        
        if (Input.GetKeyDown(KeyCode.F) && !isMovementLocked)
        {
            ToggleFlashlight();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            TestWinItemPickup();
        }
    }
    
    void TestWinItemPickup()
    {
        CollectWinItem();
        Debug.Log($"Test: Collected win item! {collectedWinItems}/{requiredWinItems}");
    }
    
    void UpdateTimers()
    {
        if (flashTimer > 0) flashTimer -= Time.deltaTime;
        if (invulnerabilityTimer > 0) invulnerabilityTimer -= Time.deltaTime;
        if (stunTimer > 0) stunTimer -= Time.deltaTime;
        if (batteryRechargeTimer > 0) batteryRechargeTimer -= Time.deltaTime;
        if (flashlightDamageTimer > 0) flashlightDamageTimer -= Time.deltaTime;
        if (damageTakenTimer > 0) damageTakenTimer -= Time.deltaTime;
        
        isInvulnerable = invulnerabilityTimer > 0;
        
        if (stunTimer <= 0) isStunned = false;
    }
    
    void UpdateHealthRegeneration()
    {
        if (!isRegenerating && damageTakenTimer <= 0 && currentHealth < maxHealth && !IsDead && !hasWon)
        {
            StartRegeneration();
        }
        
        if (isRegenerating)
        {
            float healAmount = healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
            
            OnHealed?.Invoke(healAmount);
            
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
        damageTakenTimer = healthRegenDelay;
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
        if (IsDead || hasWon) return;
        
        if (isFlashlightOn && flashlight != null && flashlight.IsActive && currentBattery > 0)
        {
            float drainAmount = batteryDrainRate * Time.deltaTime;
            ConsumeCharge(drainAmount);
            batteryRechargeTimer = batteryRechargeDelay;
            isBatteryDead = false;
        }
        else if (!isFlashlightOn && !IsFullyCharged && batteryRechargeTimer <= 0)
        {
            float rechargeAmount = batteryRechargeRate * Time.deltaTime;
            Recharge(rechargeAmount);
        }
        
        if (currentBattery <= 0 && isFlashlightOn && !isBatteryDead)
        {
            isBatteryDead = true;
            TurnFlashlightOff();
            OnBatteryEmpty?.Invoke();
        }
        
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
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            playerSprite.color = Color.Lerp(originalColor, regenColor, pulse * 0.3f);
        }
        else if (isBatteryLow && isFlashlightOn)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            playerSprite.color = Color.Lerp(originalColor, batteryLowColor, pulse * 0.3f);
        }
        else if (hasWon)
        {
            float pulse = Mathf.PingPong(Time.time * 3f, 1f);
            playerSprite.color = Color.Lerp(originalColor, Color.yellow, pulse * 0.5f);
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
        playerAnimator.SetBool("HasWon", hasWon);
        
        if (IsMoving)
        {
            playerAnimator.SetFloat("MoveX", movementInput.x);
            playerAnimator.SetFloat("MoveY", movementInput.y);
        }
    }
    
    #region Flashlight Control
    
    public void ToggleFlashlight()
    {
        if (flashlight == null || isBatteryDead || hasWon) return;
        
        if (isFlashlightOn)
        {
            TurnFlashlightOff();
        }
        else
        {
            TurnFlashlightOn();
        }
        
        if (audioSource != null && flashlightToggleSound != null)
            audioSource.PlayOneShot(flashlightToggleSound, 0.5f);
        
        OnFlashlightToggled?.Invoke(isFlashlightOn);
    }
    
    public void TurnFlashlightOn()
    {
        if (flashlight == null || currentBattery <= 0 || hasWon) return;
        
        isFlashlightOn = true;
        if (flashlight != null)
            flashlight.SetActive(true);
    }
    
    public void TurnFlashlightOff()
    {
        if (flashlight == null || hasWon) return;
        
        isFlashlightOn = false;
        if (flashlight != null)
            flashlight.SetActive(false);
        
        batteryRechargeTimer = batteryRechargeDelay;
    }
    
    public void SetFlashlightActive(bool active)
    {
        if (active) TurnFlashlightOn();
        else TurnFlashlightOff();
    }
    
    #endregion
    
    #region Win Item System
    
    public void CollectWinItem()
    {
        if (hasWon || IsDead) return;
        
        collectedWinItems++;
        Debug.Log($"Collected win item! {collectedWinItems}/{requiredWinItems}");
        
        if (audioSource != null && winItemPickupSound != null)
            audioSource.PlayOneShot(winItemPickupSound, 0.7f);
        
        OnWinItemCollected?.Invoke(collectedWinItems);
        
        if (collectedWinItems >= requiredWinItems)
        {
            WinGame();
        }
    }
    
    private void WinGame()
    {
        hasWon = true;
        
        isMovementLocked = true;
        if (rb != null)
            rb.velocity = Vector2.zero;
        
        TurnFlashlightOff();
        
        StopRegeneration();
        
        if (playerSprite != null)
        {
            playerSprite.color = Color.yellow;
        }
        
        OnWinConditionMet?.Invoke();
        
        Debug.Log("Player won the game!");
    }
    
    #endregion
    
    #region IDamageable Implementation
    
    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0 || isInvulnerable || hasWon) return;
        
        float oldHealth = currentHealth;
        CurrentHealth -= damage;
        float actualDamage = oldHealth - currentHealth;
        
        flashTimer = flashDuration;
        if (playerSprite != null)
            playerSprite.color = damageFlashColor;
        
        if (damageParticles != null)
            damageParticles.Play();
        
        if (audioSource != null && damageSound != null)
            audioSource.PlayOneShot(damageSound, 0.7f);
        
        ResetRegenerationTimer();
        
        OnDamageTaken?.Invoke(actualDamage);
        
        invulnerabilityTimer = invulnerabilityDuration;
        
        if (CurrentHealth <= 0)
        {
            OnDeath?.Invoke();
        }

        if (collectedWinItems >= requiredWinItems)
        {
            OnWinConditionMet?.Invoke();
        }
    }
    
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0 || hasWon) return;
        
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
        isMovementLocked = true;
        if (rb != null)
            rb.velocity = Vector2.zero;
        
        if (playerSprite != null)
            playerSprite.color = Color.gray;
        
        TurnFlashlightOff();
        
        StopRegeneration();
        
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound, 1f);
        
        OnDeath?.Invoke();
        
        Debug.Log("Player died!");
    }
    
    #endregion
    
    #region IRechargeable Implementation
    
    public void Recharge(float amount)
    {
        if (amount <= 0 || IsFullyCharged || hasWon) return;
        
        float oldBattery = currentBattery;
        CurrentCharge += amount;
        float actualRecharge = currentBattery - oldBattery;
        
        if (batteryParticles != null && actualRecharge > 0)
            batteryParticles.Play();
        
        if (actualRecharge > 10f && audioSource != null && batteryLowSound != null)
            audioSource.PlayOneShot(batteryLowSound, 0.3f);
        
        OnChargeChanged?.Invoke(currentBattery);
        
        if (IsFullyCharged)
        {
            OnFullyCharged?.Invoke();
        }
        
        if (currentBattery > 0 && isBatteryDead)
        {
            isBatteryDead = false;
        }
    }
    
    public void ConsumeCharge(float amount)
    {
        if (amount <= 0 || hasWon) return;
        
        float oldBattery = currentBattery;
        CurrentCharge -= amount;
        float actualConsumed = Mathf.Max(0, oldBattery - currentBattery);
        
        OnChargeChanged?.Invoke(currentBattery);
        OnChargeConsumed?.Invoke(actualConsumed);
    }
    
    public void SetMaxCharge(float newMax)
    {
        if (newMax <= 0 || hasWon) return;
        
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
        if (IsDead || hasWon) return;
        
        isStunned = true;
        stunTimer = duration;
        
        if (rb != null)
            rb.velocity = Vector2.zero;
    }
    
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (rb == null || IsDead || hasWon) return;
        
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
        if (collision.gameObject.CompareTag("Enemy"))
        {
            BaseEnemy enemy = collision.gameObject.GetComponent<BaseEnemy>();
            if (enemy != null && enemy.IsRevealed)
            {
                TakeDamage(enemy.damageOnContact);
                
                Vector2 knockbackDirection = (transform.position - collision.transform.position).normalized;
                ApplyKnockback(knockbackDirection, 5f);
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon || IsDead) return;
        
        if (other.CompareTag("BatteryPickup"))
        {
            Recharge(50f);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("WinItem"))
        {
            CollectWinItem();
            Destroy(other.gameObject);
        }
    }
    
    #endregion
    
    #region Debug GUI
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        
        if (hasWon)
        {
            GUILayout.Label("VICTORY!", new GUIStyle() { fontSize = 24, normal = { textColor = Color.yellow }, fontStyle = FontStyle.Bold });
        }
        else if (IsDead)
        {
            GUILayout.Label("GAME OVER", new GUIStyle() { fontSize = 24, normal = { textColor = Color.red }, fontStyle = FontStyle.Bold });
        }
        else
        {
            GUILayout.Label($"Health: {currentHealth:F0}/{maxHealth:F0}");
            GUILayout.Label($"Battery: {currentBattery:F0}/{maxBattery:F0}");
            GUILayout.Label($"Flashlight: {(isFlashlightOn ? "ON" : "OFF")}");
            GUILayout.Label($"Win Items: {collectedWinItems}/{requiredWinItems}");
            GUILayout.Label($"Regen Timer: {damageTakenTimer:F1}s");
            GUILayout.Label($"Controls: F-Toggle Flashlight");
            
            if (isRegenerating)
            {
                GUILayout.Label("Regenerating...", new GUIStyle() { normal = { textColor = Color.green } });
            }
        }
        
        GUILayout.EndArea();
    }
    
    #endregion
}