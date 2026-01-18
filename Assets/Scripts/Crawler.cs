using UnityEngine;
using System.Collections;

public class Crawler : BaseEnemy
{
    [Header("Crawler Specific Settings")]
    [SerializeField] private float armor = 1f;
    [SerializeField] private float slowAuraRange = 3f;
    [SerializeField] private float slowEffect = 0.3f;
    [SerializeField] private float groundPoundCooldown = 8f;
    [SerializeField] private float groundPoundRange = 4f;
    [SerializeField] private float groundPoundDamage = 25f;
    [SerializeField] private float groundPoundWindup = 1.5f;
    
    private float groundPoundTimer = 0f;
    private bool isChargingGroundPound = false;
    private bool isPerformingGroundPound = false;
    private PlayerController affectedPlayer;
    
    protected override void Start()
    {
        base.Start();
        
        maxHealth *= 2.5f;
        currentHealth = maxHealth;
        moveSpeed *= 0.4f;
        chaseSpeed *= 0.6f;
        damageOnContact *= 1.2f;
        
        groundPoundTimer = Random.Range(0f, 3f);
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (IsDead || !isActive) return;
        
        UpdateGroundPound();
        UpdateSlowAura();
    }
    
    protected override void ChaseBehavior()
    {
        if (playerTransform != null && !isChargingGroundPound && !isPerformingGroundPound)
        {
            movementDirection = (playerTransform.position - transform.position).normalized;
            isMoving = true;
            
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (groundPoundTimer <= 0 && distanceToPlayer <= groundPoundRange * 1.5f)
            {
                StartGroundPound();
            }
        }
    }
    
    protected override void RevealedBehavior()
    {
        ChaseBehavior();
    }
    
    protected override void HiddenBehavior()
    {
        Wander();
        
        if (Random.value < 0.002f)
        {
            isMoving = false;
        }
    }
    
    protected override void OnPlayerContact(GameObject player)
    {
        if (!canAttack) return;
        
        DealDamageToPlayer(damageOnContact);
        
        PlayerController playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null)
        {
            Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
            playerCtrl.ApplyKnockback(knockbackDirection, 8f);
        }
        
        if (rb != null)
        {
            Vector2 selfKnockback = (transform.position - player.transform.position).normalized;
            rb.AddForce(selfKnockback * 3f, ForceMode2D.Impulse);
        }
        
        StartCoroutine(AttackCooldown());
    }
    
    private void UpdateGroundPound()
    {
        if (groundPoundTimer > 0)
        {
            groundPoundTimer -= Time.deltaTime;
        }
        
        if (isChargingGroundPound)
        {
            if (rb != null)
                rb.velocity *= 0.9f;
            
            spriteRenderer.color = Color.Lerp(revealedColor, Color.yellow, Mathf.PingPong(Time.time * 3f, 1f));
            
            transform.position += (Vector3)Random.insideUnitCircle * 0.05f;
        }
    }
    
    private void UpdateSlowAura()
    {
        if (playerTransform == null || !isRevealed) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= slowAuraRange && HasLineOfSightToPlayer)
        {
            ApplySlowAura();
        }
        else if (affectedPlayer != null)
        {
            RemoveSlowAura();
        }
    }
    
    private void StartGroundPound()
    {
        isChargingGroundPound = true;
        isMoving = false;
        
        if (animator != null)
        {
            animator.SetTrigger("ChargeGroundPound");
        }
        
        Invoke(nameof(ExecuteGroundPound), groundPoundWindup);
    }
    
    private void ExecuteGroundPound()
    {
        isChargingGroundPound = false;
        isPerformingGroundPound = true;
        groundPoundTimer = groundPoundCooldown;
        
        if (animator != null)
        {
            animator.SetTrigger("GroundPound");
        }
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, groundPoundRange, playerLayer);
        foreach (Collider2D hit in hits)
        {
            DealDamageToPlayer(groundPoundDamage);
            
            PlayerController player = hit.GetComponent<PlayerController>();
            if (player != null)
            {
                player.Stun(1f);
                
                Vector2 knockbackDirection = (hit.transform.position - transform.position).normalized;
                player.ApplyKnockback(knockbackDirection, 10f);
            }
        }
        
        Invoke(nameof(ResetAfterGroundPound), 1f);
    }
    
    private void ResetAfterGroundPound()
    {
        isPerformingGroundPound = false;
    }
    
    private void ApplySlowAura()
    {
        if (playerController == null) return;
        
        affectedPlayer = playerController;
        
        float originalSpeed = 8f; 
        float slowedSpeed = originalSpeed * (1f - slowEffect);
        playerController.SetMoveSpeed(slowedSpeed);
    }
    
    private void RemoveSlowAura()
    {
        if (affectedPlayer == null) return;
        
        affectedPlayer.ResetMoveSpeed();
        
        affectedPlayer = null;
    }
    
    public override void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0) return;
        
        float reducedDamage = Mathf.Max(0, damage - armor);
        
        base.TakeDamage(reducedDamage);
        
        if (armor > 0 && damage > 0)
        {
            StartCoroutine(FlashArmorEffect());
        }
    }
    
    private IEnumerator FlashArmorEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        
        yield return new WaitForSeconds(0.1f);
        
        if (!IsDead)
        {
            spriteRenderer.color = isRevealed ? revealedColor : hiddenColor;
        }
    }
    
    protected override void Die()
    {
        if (affectedPlayer != null)
        {
            RemoveSlowAura();
        }
        
        isChargingGroundPound = false;
        isPerformingGroundPound = false;
        
        base.Die();
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, groundPoundRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, slowAuraRange);
    }
}