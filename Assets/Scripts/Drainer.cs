using UnityEngine;
using System.Collections;

public class Drainer : BaseEnemy
{
    [Header("Drainer Specific Settings")]
    [SerializeField] private float drainRange = 4f;
    [SerializeField] private float drainRate = 15f;
    [SerializeField] private float drainHealMultiplier = 0.3f;
    [SerializeField] private float batteryExplosionThreshold = 40f;
    [SerializeField] private float batteryExplosionDamage = 30f;
    [SerializeField] private float batteryExplosionRange = 6f;
    
    [Header("Drain Beam")]
    [SerializeField] private LineRenderer drainBeam;
    [SerializeField] private float beamWidth = 0.1f;
    
    [Header("Overcharge")]
    [SerializeField] private float overchargeDamageMultiplier = 2f;
    [SerializeField] private float overchargeDuration = 5f;
    
    private bool isDraining = false;
    private float currentDrainAmount = 0f;
    private bool isOvercharged = false;
    private float overchargeTimer = 0f;
    
    protected override void Start()
    {
        base.Start();
        
        maxHealth *= 0.8f;
        currentHealth = maxHealth;
        moveSpeed *= 0.8f;
        chaseSpeed *= 0.9f;
        
        if (drainBeam == null)
        {
            CreateDrainBeam();
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (IsDead || !isActive) return;
        
        UpdateDrain();
        UpdateOvercharge();
    }
    
    protected override void ChaseBehavior()
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= drainRange && HasLineOfSightToPlayer && playerController != null)
            {
                if (!isDraining && playerController.BatteryPercentage > 0)
                {
                    StartDraining();
                }
                
                isMoving = false;
                movementDirection = (playerTransform.position - transform.position).normalized;
            }
            else
            {
                if (isDraining)
                {
                    StopDraining();
                }
                
                movementDirection = (playerTransform.position - transform.position).normalized;
                isMoving = true;
                
                if (playerController != null && playerController.BatteryPercentage < 0.3f)
                {
                    chaseSpeed = base.chaseSpeed * 1.5f;
                }
                else
                {
                    chaseSpeed = base.chaseSpeed;
                }
            }
        }
    }
    
    protected override void RevealedBehavior()
    {
        ChaseBehavior();
        
        if (isRevealed && isDraining)
        {
            drainRate = 25f;
        }
    }
    
    protected override void HiddenBehavior()
    {
        if (playerTransform != null && playerController != null)
        {
            float batteryAttraction = playerController.BatteryPercentage * 2f;
            
            if (Random.value < 0.01f * batteryAttraction)
            {
                movementDirection = (playerTransform.position - transform.position).normalized;
                isMoving = true;
            }
            else
            {
                Wander();
            }
            
            if (Vector2.Distance(transform.position, playerTransform.position) < 2f && 
                playerController.BatteryPercentage > 0.5f)
            {
                Reveal();
            }
        }
        else
        {
            Wander();
        }
    }
    
    protected override void OnPlayerContact(GameObject player)
    {
        if (!canAttack) return;
        
        DealDamageToPlayer(damageOnContact);
        
        if (playerController != null)
        {
            DrainPlayerBattery(damageOnContact * 3f);
        }
        
        StartCoroutine(AttackCooldown());
    }
    
    private void CreateDrainBeam()
    {
        GameObject beamObject = new GameObject("DrainBeam");
        beamObject.transform.SetParent(transform);
        beamObject.transform.localPosition = Vector3.zero;
        
        drainBeam = beamObject.AddComponent<LineRenderer>();
        drainBeam.startWidth = beamWidth;
        drainBeam.endWidth = beamWidth * 1.5f;
        drainBeam.material = new Material(Shader.Find("Sprites/Default"));
        drainBeam.positionCount = 2;
        drainBeam.enabled = false;
    }
    
    private void UpdateDrain()
    {
        if (!isDraining || playerTransform == null || playerController == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > drainRange || !HasLineOfSightToPlayer || playerController.BatteryPercentage <= 0)
        {
            StopDraining();
            return;
        }
        
        float drainThisFrame = drainRate * Time.deltaTime;
        if (isOvercharged)
        {
            drainThisFrame *= overchargeDamageMultiplier;
        }
        
        DrainPlayerBattery(drainThisFrame);
        currentDrainAmount += drainThisFrame;
        
        float healAmount = drainThisFrame * drainHealMultiplier;
        Heal(healAmount);
        
        UpdateDrainBeam();
        
        if (currentDrainAmount >= batteryExplosionThreshold)
        {
            TriggerBatteryExplosion();
        }
    }
    
    private void UpdateDrainBeam()
    {
        if (drainBeam == null || playerTransform == null) return;
        
        drainBeam.SetPosition(0, transform.position);
        drainBeam.SetPosition(1, playerTransform.position);
        
        float pulse = Mathf.PingPong(Time.time * 3f, 1f);
        drainBeam.startWidth = beamWidth + pulse * 0.05f;
        drainBeam.endWidth = beamWidth * 1.5f + pulse * 0.1f;
        
        Color beamColor = Color.Lerp(Color.cyan, Color.magenta, currentDrainAmount / batteryExplosionThreshold);
        drainBeam.startColor = beamColor;
        drainBeam.endColor = beamColor * 1.5f;
    }
    
    private void UpdateOvercharge()
    {
        if (isOvercharged)
        {
            overchargeTimer -= Time.deltaTime;
            
            spriteRenderer.color = Color.Lerp(revealedColor, Color.white, Mathf.PingPong(Time.time * 5f, 1f));
            
            if (overchargeTimer <= 0)
            {
                EndOvercharge();
            }
        }
    }
    
    private void StartDraining()
    {
        isDraining = true;
        currentDrainAmount = 0f;
        
        if (animator != null)
        {
            animator.SetBool("IsDraining", true);
        }
        
        if (drainBeam != null)
        {
            drainBeam.enabled = true;
        }
    }
    
    private void StopDraining()
    {
        isDraining = false;
        
        if (animator != null)
        {
            animator.SetBool("IsDraining", false);
        }
        
        if (drainBeam != null)
        {
            drainBeam.enabled = false;
        }
        
        drainRate = 15f;
    }
    
    private void TriggerBatteryExplosion()
    {
        currentDrainAmount = 0f;
        
        DealDamageToPlayer(batteryExplosionDamage);
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, batteryExplosionRange);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy") && hit.gameObject != gameObject)
            {
                BaseEnemy otherEnemy = hit.GetComponent<BaseEnemy>();
                if (otherEnemy != null)
                {
                    otherEnemy.TakeDamage(batteryExplosionDamage * 0.5f);
                }
            }
        }
        
        StartOvercharge();
        StartCoroutine(DrainerStun());
    }
    
    private void StartOvercharge()
    {
        isOvercharged = true;
        overchargeTimer = overchargeDuration;
        
        moveSpeed *= 1.5f;
        chaseSpeed *= 1.5f;
        damageOnContact *= 1.5f;
    }
    
    private void EndOvercharge()
    {
        isOvercharged = false;
        
        moveSpeed = base.moveSpeed;
        chaseSpeed = base.chaseSpeed;
        damageOnContact = base.damageOnContact;
    }
    
    private IEnumerator DrainerStun()
    {
        StopDraining();
        isMoving = false;
        if (rb != null) rb.velocity = Vector2.zero;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        
        yield return new WaitForSeconds(1f);
        
        spriteRenderer.color = originalColor;
    }
    
    public override void TakeDamage(float damage)
    {
        if (isDraining && currentDrainAmount > 0)
        {
            float explosionDamage = currentDrainAmount * 0.5f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, batteryExplosionRange * 0.5f);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    DealDamageToPlayer(explosionDamage);
                }
            }
            currentDrainAmount = 0f;
        }
        
        base.TakeDamage(damage);
    }
    
    protected override void Die()
    {
        if (currentDrainAmount > 0)
        {
            TriggerBatteryExplosion();
        }
        
        StopDraining();
        base.Die();
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, drainRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, batteryExplosionRange);
        
        if (isDraining && playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}