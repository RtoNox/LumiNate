using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Flashlight")]
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform flashlightPivot; // NEW: Separate flashlight transform
    [SerializeField] private float revealDrainRate = 2f;
    [SerializeField] private float killDrainRate = 8f;
    [SerializeField] private float baseRechargeRate = 5f;
    [SerializeField] private float dangerRechargeMultiplier = 2f;
    [SerializeField] private float flashlightRange = 15f;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invincibilityDuration = 1f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip flashlightClick;
    [SerializeField] private AudioClip lowBatterySound;
    [SerializeField] private AudioClip damageSound;
    
    [Header("Visual")]
    [SerializeField] private GameObject rechargeIndicator;
    [SerializeField] private Material flashMaterial;
    [SerializeField] private bool rotatePlayerWithMouse = false; // NEW: Toggle player rotation
    
    
    // Components
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private Material originalMaterial;
    private SpriteRenderer spriteRenderer;
    
    // Systems
    private BatterySystem batterySystem;
    private float currentHealth;
    private bool isInvincible = false;
    
    // State
    private FlashlightMode currentMode = FlashlightMode.Off;
    private bool isInDanger = false;
    private Vector2 movementInput;
    private Vector2 mousePosition;
    
    // Properties
    public BatterySystem Battery => batterySystem;
    public Light Flashlight => flashlight;
    public float RevealDrainRate => revealDrainRate;
    public float KillDrainRate => killDrainRate;
    public float BaseRechargeRate => baseRechargeRate;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    public bool IsInvincible => isInvincible;
    
    // Events
    public event System.Action OnDeath;
    public event System.Action<float> OnHealthChanged;
    public event System.Action<FlashlightMode> OnFlashlightModeChanged;
    
    public enum FlashlightMode { Off, Reveal, Kill }
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        
        // Create flashlight pivot if not assigned
        if (flashlightPivot == null && flashlight != null)
        {
            GameObject pivot = new GameObject("FlashlightPivot");
            pivot.transform.parent = transform;
            pivot.transform.localPosition = Vector3.zero;
            flashlight.transform.parent = pivot.transform;
            flashlight.transform.localPosition = new Vector3(0.5f, 0, 0); // Position in front
            flashlight.transform.localRotation = Quaternion.identity;
            flashlightPivot = pivot.transform;
        }
        
        InitializeSystems();
        GetOriginalMaterial();
        
        // Configure Rigidbody2D
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
    
    void Start()
    {
        GameManager.Instance.RegisterPlayer(this);
        UIManager.Instance.UpdateBattery(batterySystem.CurrentCharge / batterySystem.MaxCharge);
        UIManager.Instance.UpdateHealth(currentHealth / maxHealth);
    }
    
    void Update()
    {
        if (IsDead) return;
        
        HandleInput();
        HandleFlashlight();
        HandleRecharge();
        CheckForEnemies();
        
        batterySystem.Update(Time.deltaTime);
        
        // Update camera position
        UpdateCamera();
    }
    
    void FixedUpdate()
    {
        if (IsDead) return;
        
        HandleMovement();
    }
    
    void InitializeSystems()
    {
        batterySystem = new BatterySystem(100f, 30f);
        batterySystem.OnChargeChanged += OnBatteryChargeChanged;
        batterySystem.OnRechargeStateChanged += OnRechargeStateChanged;
        
        currentHealth = maxHealth;
        
        SetFlashlightMode(FlashlightMode.Off);
    }
    
    void GetOriginalMaterial()
    {
        if (spriteRenderer != null)
            originalMaterial = spriteRenderer.material;
    }
    
    void HandleInput()
    {
        // Movement input
        movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
        
        // Get mouse position for flashlight rotation
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Rotate flashlight to face mouse
        RotateFlashlightToMouse();
        
        // Optionally rotate player sprite (if enabled)
        if (rotatePlayerWithMouse)
        {
            RotatePlayerToMouse();
        }
    }
    
    void RotateFlashlightToMouse()
    {
        if (flashlightPivot == null) return;
        
        // Calculate direction to mouse
        Vector2 direction = mousePosition - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Rotate flashlight pivot
        flashlightPivot.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    void RotatePlayerToMouse()
    {
        // Only rotate if enabled
        Vector2 direction = mousePosition - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    void HandleMovement()
    {
        if (movementInput.magnitude > 0.1f)
        {
            rb.velocity = movementInput * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    void UpdateCamera()
    {
        if (cameraTransform != null)
        {
            Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, cameraTransform.position.z);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, cameraFollowSpeed * Time.deltaTime);
        }
    }
    
    void HandleFlashlight()
    {
        // Toggle flashlight on/off with Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentMode == FlashlightMode.Off)
                SetFlashlightMode(FlashlightMode.Reveal);
            else
                SetFlashlightMode(FlashlightMode.Off);
        }
        
        // Switch modes with Right Click
        if (Input.GetMouseButtonDown(1) && currentMode != FlashlightMode.Off)
        {
            FlashlightMode nextMode = currentMode == FlashlightMode.Reveal ? 
                FlashlightMode.Kill : FlashlightMode.Reveal;
            SetFlashlightMode(nextMode);
        }
        
        // Drain battery when light is on
        if (currentMode != FlashlightMode.Off)
        {
            float drainRate = currentMode == FlashlightMode.Reveal ? revealDrainRate : killDrainRate;
            batterySystem.Drain(drainRate * Time.deltaTime);
            
            CheckForEnemiesInLight();
            
            if (batterySystem.CurrentCharge <= 0)
                SetFlashlightMode(FlashlightMode.Off);
        }
    }
    
    void SetFlashlightMode(FlashlightMode mode)
    {
        if (currentMode == mode) return;
        
        currentMode = mode;
        
        switch (mode)
        {
            case FlashlightMode.Off:
                if (flashlight != null)
                    flashlight.enabled = false;
                batterySystem.StopRecharge();
                break;
                
            case FlashlightMode.Reveal:
                if (flashlight != null)
                {
                    flashlight.enabled = true;
                    flashlight.spotAngle = 60f;
                    flashlight.color = Color.white;
                    flashlight.intensity = 1.5f;
                }
                batterySystem.StopRecharge();
                PlaySound(flashlightClick);
                break;
                
            case FlashlightMode.Kill:
                if (flashlight != null)
                {
                    flashlight.enabled = true;
                    flashlight.spotAngle = 30f;
                    flashlight.color = new Color(0.3f, 0.6f, 1f);
                    flashlight.intensity = 3f;
                }
                batterySystem.StopRecharge();
                PlaySound(flashlightClick);
                break;
        }
        
        OnFlashlightModeChanged?.Invoke(mode);
    }
    
    void CheckForEnemiesInLight()
    {
        if (currentMode == FlashlightMode.Off || flashlightPivot == null) return;
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, flashlightRange);
        foreach (var collider in colliders)
        {
            Vector3 direction = collider.transform.position - transform.position;
            
            // Use flashlight forward direction
            float angle = Vector3.Angle(flashlightPivot.right, direction); // flashlight forward is right
            
            if (angle < flashlight.spotAngle * 0.5f)
            {
                IRevealable revealable = collider.GetComponent<IRevealable>();
                if (revealable != null)
                {
                    bool isKillMode = currentMode == FlashlightMode.Kill;
                    revealable.OnFlashlightHit(isKillMode);
                }
            }
        }
    }
    
    void HandleRecharge()
    {
        if (currentMode == FlashlightMode.Off && batterySystem.CurrentCharge < batterySystem.MaxCharge)
        {
            float rechargeRate = baseRechargeRate * (isInDanger ? dangerRechargeMultiplier : 1f);
            batterySystem.StartRecharge(rechargeRate);
            
            if (rechargeIndicator != null)
            {
                rechargeIndicator.SetActive(true);
                rechargeIndicator.GetComponent<SpriteRenderer>().color = 
                    isInDanger ? Color.red : Color.green;
            }
        }
        else
        {
            if (rechargeIndicator != null)
                rechargeIndicator.SetActive(false);
        }
    }
    
    void CheckForEnemies()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 5f);
        isInDanger = false;
        
        foreach (var collider in nearby)
        {
            if (collider.GetComponent<BaseEnemy>() != null)
            {
                isInDanger = true;
                break;
            }
        }
    }
    
    void OnBatteryChargeChanged(float percentage)
    {
        UIManager.Instance.UpdateBattery(percentage);
        
        if (percentage < 0.1f && !audioSource.isPlaying)
            PlaySound(lowBatterySound);
    }
    
    void OnRechargeStateChanged(bool isRecharging)
    {
        UIManager.Instance.ShowRechargeStatus(isRecharging, 
            isRecharging ? baseRechargeRate * (isInDanger ? dangerRechargeMultiplier : 1f) : 0);
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
    
    public void TakeDamage(float damage)
    {
        if (IsDead || isInvincible) return;
        
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        UIManager.Instance.UpdateHealth(currentHealth / maxHealth);
        
        PlaySound(damageSound);
        StartCoroutine(DamageFlash());
        StartCoroutine(InvincibilityFrames());
        
        if (currentHealth <= 0)
            Die();
    }
    
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        UIManager.Instance.UpdateHealth(currentHealth / maxHealth);
    }
    
    IEnumerator DamageFlash()
    {
        if (spriteRenderer != null && flashMaterial != null)
        {
            spriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.material = originalMaterial;
        }
    }
    
    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }
    
    void Die()
    {
        Debug.Log("Player Died!");
        OnDeath?.Invoke();
        GameManager.Instance.GameOver(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Collectible"))
        {
            CollectItem(other.gameObject);
        }
        else if (other.CompareTag("BatteryPickup"))
        {
            BatteryPickup pickup = other.GetComponent<BatteryPickup>();
            if (pickup != null)
            {
                batterySystem.AddCharge(pickup.ChargeAmount);
                Destroy(other.gameObject);
            }
        }
    }
    
    void CollectItem(GameObject item)
    {
        Mimic mimic = item.GetComponent<Mimic>();
        if (mimic != null)
        {
            mimic.Reveal(5f);
            return;
        }
        
        GameManager.Instance.CollectItem();
        Destroy(item);
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (flashlight != null && flashlight.enabled)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, flashlightRange);
            
            // Draw flashlight direction
            if (flashlightPivot != null)
            {
                Gizmos.color = Color.red;
                Vector3 endPoint = transform.position + flashlightPivot.right * flashlightRange;
                Gizmos.DrawLine(transform.position, endPoint);
            }
        }
    }
}