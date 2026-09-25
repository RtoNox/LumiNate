using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class Flashlight : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private float coneAngle = 60f;
    [SerializeField] private float coneLength = 8f;
    
    [Header("Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionUpdateRate = 0.1f;
    
    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageMultiplier = 1f;
    
    [Header("References")]
    [SerializeField] private Light2D flashlightLight;
    [SerializeField] private ParticleSystem flashlightFX;

    private PlayerController owner;
    private bool isActive = true;
    private float lastDetectionTime;
    private List<BaseEnemy> enemiesInLight = new List<BaseEnemy>();
    
    public bool IsActive => isActive;
    
    void Start()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<PlayerController>();
        }

        if (flashlightLight == null) 
        {
            flashlightLight = GetComponent<Light2D>(); 
        }

        UpdateLightSettings(); 
        UpdateLightVisibility();
    }
    
    void Update()
    {
        UpdateLightVisibility();
        
        if (isActive && owner != null && owner.IsFlashlightOn)
        {
            if (Time.time - lastDetectionTime >= detectionUpdateRate)
            {
                DetectEnemies();
                ApplyDamageToEnemies();
                lastDetectionTime = Time.time;
            }
        }
        else
        {
            enemiesInLight.Clear();
        }
    }

    void UpdateLightVisibility()
    {
        if (flashlightLight != null)
        {
            bool shouldbeVisible =
            isActive &&
            owner != null &&
            owner.IsFlashlightOn &&
            owner.CurrentCharge > 0;

            flashlightLight.enabled = shouldbeVisible;
            flashlightFX.gameObject.SetActive(shouldbeVisible);
        }
    }

    void UpdateLightSettings()
    {
        if (flashlightLight != null)
        {
            flashlightLight.pointLightOuterAngle = coneAngle;
            flashlightLight.pointLightOuterRadius = coneLength;
        }
    }

    void DetectEnemies()
    {
        List<BaseEnemy> newEnemiesInLight = new List<BaseEnemy>();
        
        if (!isActive || owner == null || !owner.IsFlashlightOn) return;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, coneLength, enemyLayer);
        
        foreach (Collider2D hit in hits)
        {
            Vector3 directionToEnemy = hit.transform.position - transform.position;
            float distanceToEnemy = directionToEnemy.magnitude;
            
            float angleToEnemy = Vector3.Angle(transform.right, directionToEnemy);
            if (angleToEnemy <= coneAngle / 2 && distanceToEnemy <= coneLength)
            {
                RaycastHit2D losCheck = Physics2D.Raycast(
                    transform.position,
                    directionToEnemy.normalized,
                    distanceToEnemy,
                    obstacleLayer
                );
                
                if (losCheck.collider == null || losCheck.collider.gameObject == hit.gameObject)
                {
                    BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        newEnemiesInLight.Add(enemy);
                        
                        if (!enemy.IsRevealed)
                        {
                            enemy.Reveal();
                        }
                    }
                }
            }
        }
        
        enemiesInLight = newEnemiesInLight;
    }
    
    void ApplyDamageToEnemies()
    {
        if (owner == null || enemiesInLight.Count == 0) return;
        
        float damagePerUpdate = (damagePerSecond * detectionUpdateRate) * damageMultiplier;
        
        foreach (BaseEnemy enemy in enemiesInLight)
        {
            if (enemy != null && !enemy.IsDead)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                float distanceMultiplier = Mathf.Clamp01(1f - (distance / coneLength));
                
                float finalDamage = damagePerUpdate * distanceMultiplier;
                
                Debug.Log($"Damaging {enemy.name}: {finalDamage:F2} damage (DPS: {damagePerSecond}, Update: {damagePerUpdate:F2}, Dist: {distance:F1}, Multiplier: {distanceMultiplier:F2})");
                
                enemy.TakeDamage(finalDamage);
            }
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (!active)
        {
            enemiesInLight.Clear();
        }

        UpdateLightVisibility();
    }
    
    public void SetOwner(PlayerController player)
    {
        owner = player;
        UpdateLightVisibility();
    }
    
    public void SetConeAngle(float angle)
    {
        coneAngle = Mathf.Clamp(angle, 10f, 120f);
        UpdateLightSettings();
    }
    
    public void SetConeLength(float length)
    {
        coneLength = Mathf.Clamp(length, 2f, 20f);
        UpdateLightSettings();
    }
    
    public void SetDamage(float newDamagePerSecond)
    {
        damagePerSecond = Mathf.Max(0, newDamagePerSecond);
    }
    
    public List<BaseEnemy> GetEnemiesInLight() => new List<BaseEnemy>(enemiesInLight);

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.yellow;

        int segments = 20;
        float angleStep = coneAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -coneAngle / 2 + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * transform.right;
            Gizmos.DrawRay(transform.position, direction * coneLength);
        }

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