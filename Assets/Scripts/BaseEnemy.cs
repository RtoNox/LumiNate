using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseEnemy : MonoBehaviour, IRevealable, IDamageable, IWaveEntity
{
    [Header("Base Settings")]
    [SerializeField] protected string enemyName = "Enemy";
    [SerializeField] protected float baseSpeed = 3f;
    [SerializeField] protected float baseHealth = 100f;
    [SerializeField] protected float contactDamage = 10f;
    [SerializeField] protected float attackCooldown = 1f;
    
    [Header("Visual")]
    [SerializeField] protected Material visibleMaterial;
    [SerializeField] protected Material invisibleMaterial;
    
    protected float currentSpeed;
    protected float currentHealth;
    protected Transform player;
    protected Renderer enemyRenderer;
    
    private bool isVisible = false;
    private float visibilityTimer = 0f;
    private float lastAttackTime = 0f;
    private int waveNumber = 0;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth { get; protected set; }
    public bool IsDead => currentHealth <= 0;
    public bool IsVisible => isVisible;
    public int WaveNumber => waveNumber;
    
    public event System.Action OnDeath;
    public event System.Action<float> OnHealthChanged;
    
    protected virtual void InitializeEnemyType()
    {
       
    }
    
    protected virtual void UpdateMovement()
    {
        
    }
    
    protected virtual void UpdateSpecialAbility()
    {
        
    }
    
    void Start()
    {
        enemyRenderer = GetComponentInChildren<Renderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        MaxHealth = baseHealth;
        currentHealth = MaxHealth;
        currentSpeed = baseSpeed;
        
        InitializeEnemyType();
        SetInvisible();
        
        GameManager.Instance.RegisterEnemy(this);
    }
    
    protected virtual void Update()
    {
        if (IsDead || player == null) return;
        
        if (isVisible && visibilityTimer > 0)
        {
            visibilityTimer -= Time.deltaTime;
            if (visibilityTimer <= 0)
                SetInvisible();
        }
        
        if (!IsPlayerInLight())
        {
            UpdateMovement();
            UpdateSpecialAbility();
        }
        else
        {
            currentSpeed = baseSpeed * 0.5f;
            UpdateMovement();
        }
    }
    
    public virtual void Reveal(float duration)
    {
        isVisible = true;
        visibilityTimer = duration;
        SetVisible();
    }
    
    public void Hide()
    {
        SetInvisible();
    }
    
    public virtual void OnFlashlightHit(bool isKillMode)
    {
        Reveal(3f);
        
        if (isKillMode)
        {
            TakeDamage(25f * Time.deltaTime);
        }
    }
    
    void SetVisible()
    {
        isVisible = true;
        if (enemyRenderer != null && visibleMaterial != null)
            enemyRenderer.material = visibleMaterial;
    }
    
    void SetInvisible()
    {
        isVisible = false;
        if (enemyRenderer != null && invisibleMaterial != null)
            enemyRenderer.material = invisibleMaterial;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);
        
        if (currentHealth <= 0)
            Die();
    }
    
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth / MaxHealth);
    }
    
    void Die()
    {
        OnDeath?.Invoke();
        
        if (Random.value < 0.3f)
            SpawnBatteryPickup();
        
        GameManager.Instance.RemoveEnemy(this);
        Destroy(gameObject);
    }
    
    void SpawnBatteryPickup()
    {
        GameObject battery = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        battery.transform.position = transform.position + Vector3.up;
        battery.transform.localScale = Vector3.one * 0.3f;
        BatteryPickup pickup = battery.AddComponent<BatteryPickup>();
        pickup.SetChargeAmount(25f);
        battery.GetComponent<Renderer>().material.color = Color.green;
        battery.tag = "BatteryPickup";
    }

    public void InitializeWave(int waveNum)
    {
        waveNumber = waveNum;
        ScaleWithWave(waveNum);
    }
    
    public void OnWaveStart()
    {
        gameObject.SetActive(true);
    }
    
    public void OnWaveEnd()
    {
        //currently not using
    }
    
    protected virtual void ScaleWithWave(int waveNum)
    {
        float multiplier = 1 + (waveNum * 0.2f);
        MaxHealth *= multiplier;
        currentHealth = MaxHealth;
        contactDamage *= multiplier;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsDead) return;
        
        PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
        if (playerController != null && Time.time > lastAttackTime + attackCooldown)
        {
            playerController.TakeDamage(contactDamage);
            lastAttackTime = Time.time;
            OnAttackPlayer();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (IsDead) return;
        
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController != null && Time.time > lastAttackTime + attackCooldown)
        {
            playerController.TakeDamage(contactDamage);
            lastAttackTime = Time.time;
            OnAttackPlayer();
        }
    }
    
    protected virtual void OnAttackPlayer()
    {
        Debug.Log($"{enemyName} attacked player!");
    }

    protected bool IsPlayerInLight()
    {
        if (player == null) return false;
        
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null || !pc.Flashlight.enabled) return false;
        
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        float angle = Vector3.Angle(pc.transform.forward, -toPlayer);
        
        return distance < pc.Flashlight.range && angle < pc.Flashlight.spotAngle * 0.5f;
    }
    
    protected void MoveTowardPlayer()
    {
        if (player == null) return;
        
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * currentSpeed * Time.deltaTime;
        transform.LookAt(player.position);
    }
}