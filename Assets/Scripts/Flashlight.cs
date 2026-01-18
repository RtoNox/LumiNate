using UnityEngine;
using System.Collections.Generic;

public class Flashlight : MonoBehaviour, IRechargeable
{
    [Header("Flashlight Settings")]
    [SerializeField] private float coneAngle = 60f;
    [SerializeField] private float coneLength = 8f;
    [SerializeField] private Gradient coneGradient;
    
    [Header("Battery Settings")]
    [SerializeField] private float maxCharge = 100f;
    [SerializeField] private float currentCharge = 100f;
    
    [Header("Detection & Damage")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionUpdateRate = 0.1f;
    [SerializeField] private float damageMultiplier = 1f;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem flashlightParticles;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color lowBatteryColor = Color.yellow;
    
    // Components
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private PlayerController owner;
    private float lastDetectionTime;
    
    // State
    private bool isActive = true;
    private Color currentConeColor;
    private List<BaseEnemy> enemiesInLight = new List<BaseEnemy>();
    
    // Events
    public event System.Action<float> OnChargeChanged;
    public event System.Action<float> OnChargeConsumed;
    public event System.Action OnFullyCharged;
    
    // Interface Properties
    public float CurrentCharge { get => currentCharge; private set => currentCharge = Mathf.Clamp(value, 0, maxCharge); }
    public float MaxCharge { get => maxCharge; private set => maxCharge = Mathf.Max(0, value); }
    public bool IsFullyCharged { get => currentCharge >= maxCharge; }
    public bool IsActive => isActive;
    
    void Start()
    {
        InitializeComponents();
        CreateFlashlightMesh();
        
        currentConeColor = activeColor;
        
        // Setup material
        Material material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material = material;
        
        // Setup particles
        if (flashlightParticles != null)
        {
            var main = flashlightParticles.main;
            main.startColor = activeColor;
        }
    }
    
    void Update()
    {
        UpdateMeshVisibility();
        UpdateConeColor();
        
        if (isActive && Time.time - lastDetectionTime >= detectionUpdateRate)
        {
            DetectEnemies();
            lastDetectionTime = Time.time;
        }
        
        // Smoothly rotate to follow mouse (handled by PlayerController)
    }
    
    void InitializeComponents()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }
    
    void CreateFlashlightMesh()
    {
        int rayCount = 30;
        Vector3[] vertices = new Vector3[rayCount + 2];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[rayCount * 3];
        
        // Origin point
        vertices[0] = Vector3.zero;
        colors[0] = coneGradient.Evaluate(0);
        colors[0] *= currentConeColor;
        
        // Create cone
        for (int i = 0; i <= rayCount; i++)
        {
            float angle = -coneAngle / 2 + (coneAngle / rayCount) * i;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
            
            vertices[i + 1] = direction * coneLength;
            
            float t = (float)i / rayCount;
            Color gradientColor = coneGradient.Evaluate(t);
            colors[i + 1] = gradientColor * currentConeColor;
            
            if (i < rayCount)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
    }
    
    void UpdateMeshVisibility()
    {
        meshRenderer.enabled = isActive && currentCharge > 0;
        
        if (flashlightParticles != null)
        {
            if (isActive && currentCharge > 0)
            {
                if (!flashlightParticles.isPlaying)
                    flashlightParticles.Play();
            }
            else
            {
                if (flashlightParticles.isPlaying)
                    flashlightParticles.Stop();
            }
        }
    }
    
    void UpdateConeColor()
    {
        if (owner == null) return;
        
        Color targetColor = activeColor;
        
        // Change color based on battery level
        if (owner.BatteryPercentage <= 0.2f)
        {
            // Pulse between yellow and white when battery is low
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            targetColor = Color.Lerp(lowBatteryColor, activeColor, pulse);
        }
        else if (!isActive || currentCharge <= 0)
        {
            targetColor = Color.gray;
        }
        
        // Smoothly transition colors
        currentConeColor = Color.Lerp(currentConeColor, targetColor, Time.deltaTime * 5f);
        
        // Update particle color
        if (flashlightParticles != null)
        {
            var main = flashlightParticles.main;
            main.startColor = currentConeColor;
        }
        
        // Update mesh colors
        UpdateMeshColors();
    }
    
    void UpdateMeshColors()
    {
        if (mesh.colors.Length == 0) return;
        
        Color[] colors = mesh.colors;
        colors[0] = coneGradient.Evaluate(0) * currentConeColor;
        
        for (int i = 1; i < colors.Length; i++)
        {
            float t = (float)(i - 1) / (colors.Length - 2);
            Color gradientColor = coneGradient.Evaluate(t);
            colors[i] = gradientColor * currentConeColor;
        }
        
        mesh.colors = colors;
    }
    
    void DetectEnemies()
    {
        // Clear previous list
        enemiesInLight.Clear();
        
        if (!isActive || currentCharge <= 0) return;
        
        // Check for enemies in cone
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, coneLength, enemyLayer);
        
        foreach (Collider2D hit in hits)
        {
            Vector3 directionToEnemy = hit.transform.position - transform.position;
            
            // Check if enemy is within cone angle
            float angleToEnemy = Vector3.Angle(transform.right, directionToEnemy);
            if (angleToEnemy <= coneAngle / 2)
            {
                // Check line of sight
                RaycastHit2D losCheck = Physics2D.Raycast(
                    transform.position,
                    directionToEnemy.normalized,
                    directionToEnemy.magnitude,
                    obstacleLayer
                );
                
                if (losCheck.collider == null || losCheck.collider.gameObject == hit.gameObject)
                {
                    BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
                    if (enemy != null)
                    {
                        enemiesInLight.Add(enemy);
                        
                        // Reveal enemy
                        if (!enemy.IsRevealed)
                        {
                            enemy.Reveal();
                        }
                    }
                }
            }
        }
    }
    
    public void DamageEnemiesInLight()
    {
        if (!isActive || currentCharge <= 0 || owner == null) return;
        
        foreach (BaseEnemy enemy in enemiesInLight)
        {
            if (enemy != null && !enemy.IsDead)
            {
                // Calculate damage based on distance (more damage up close)
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                float distanceMultiplier = Mathf.Clamp01(1f - (distance / coneLength));
                
                float damage = owner.FlashlightRevealDamage * damageMultiplier * Time.deltaTime;
                
                // Apply damage
                enemy.TakeDamage(damage);
                
                // Visual feedback
                enemy.FlashlightHit(transform.position);
            }
        }
    }
    
    #region IRechargeable Implementation
    
    public void Recharge(float amount)
    {
        if (amount <= 0 || IsFullyCharged) return;
        
        float oldCharge = currentCharge;
        CurrentCharge += amount;
        float actualRecharge = currentCharge - oldCharge;
        
        OnChargeChanged?.Invoke(currentCharge);
        
        if (IsFullyCharged)
        {
            OnFullyCharged?.Invoke();
        }
    }
    
    public void ConsumeCharge(float amount)
    {
        if (amount <= 0 || currentCharge <= 0) return;
        
        float oldCharge = currentCharge;
        CurrentCharge -= amount;
        float actualConsumed = oldCharge - currentCharge;
        
        OnChargeChanged?.Invoke(currentCharge);
        OnChargeConsumed?.Invoke(actualConsumed);
    }
    
    public void SetMaxCharge(float newMax)
    {
        if (newMax <= 0) return;
        
        float percentage = currentCharge / maxCharge;
        MaxCharge = newMax;
        CurrentCharge = maxCharge * percentage;
        
        OnChargeChanged?.Invoke(currentCharge);
    }
    
    #endregion
    
    #region Public Methods
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (!active && flashlightParticles != null)
        {
            flashlightParticles.Stop();
        }
    }
    
    public void SetOwner(PlayerController player)
    {
        owner = player;
        
        // Sync battery with owner
        if (owner != null)
        {
            maxCharge = owner.MaxCharge;
            currentCharge = owner.CurrentCharge;
        }
    }
    
    public void SetConeAngle(float angle)
    {
        coneAngle = Mathf.Clamp(angle, 10f, 120f);
        CreateFlashlightMesh();
    }
    
    public void SetConeLength(float length)
    {
        coneLength = Mathf.Clamp(length, 2f, 20f);
        CreateFlashlightMesh();
    }
    
    public List<BaseEnemy> GetEnemiesInLight() => new List<BaseEnemy>(enemiesInLight);
    
    #endregion
    
    #region Gizmos
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        int segments = 20;
        float angleStep = coneAngle / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = -coneAngle / 2 + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * transform.right;
            Gizmos.DrawRay(transform.position, direction * coneLength);
        }
        
        // Draw enemies in light
        Gizmos.color = Color.red;
        foreach (BaseEnemy enemy in enemiesInLight)
        {
            if (enemy != null)
            {
                Gizmos.DrawLine(transform.position, enemy.transform.position);
            }
        }
    }
    
    #endregion
}