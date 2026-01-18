using UnityEngine;
using System.Collections;

public class Mimic : BaseEnemy
{
    [Header("Mimic Specific Settings")]
    [SerializeField] private GameObject[] itemDisguises;
    [SerializeField] private float disguiseRange = 2.5f;
    [SerializeField] private float revealDelay = 0.3f;
    [SerializeField] private float surpriseAttackMultiplier = 2.5f;
    [SerializeField] private float disguiseCooldown = 12f;
    [SerializeField] private float minDisguiseTime = 6f;
    
    [Header("Surprise Attack")]
    [SerializeField] private float lungeSpeed = 12f;
    [SerializeField] private float lungeDistance = 4f;
    [SerializeField] private float lungeStunDuration = 1.2f;
    [SerializeField] private float lungeCooldown = 6f;
    
    [Header("Disguise")]
    [SerializeField] private float itemRotationSpeed = 45f;
    [SerializeField] private float itemBobHeight = 0.2f;
    [SerializeField] private float itemBobSpeed = 2f;
    
    private GameObject currentDisguise;
    private bool isDisguised = true;
    private float disguiseTimer = 0f;
    private float cooldownTimer = 0f;
    private float lungeTimer = 0f;
    private bool isLunging = false;
    private Vector2 lungeDirection;
    private Vector2 lungeStartPosition;
    private Vector3 itemStartPosition;
    
    protected override void Start()
    {
        base.Start();
        
        maxHealth *= 0.7f;
        currentHealth = maxHealth;
        moveSpeed *= 0.9f;
        chaseSpeed *= 1.1f;
        
        AssumeDisguise();
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (IsDead || !isActive) return;
        
        UpdateDisguise();
        UpdateLunge();
        UpdateItemAnimation();
        
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        
        if (lungeTimer > 0)
        {
            lungeTimer -= Time.deltaTime;
        }
    }
    
    protected override void ChaseBehavior()
    {
        if (isDisguised || isLunging) return;
        
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= disguiseRange && !isRevealed)
            {
                StartCoroutine(RevealAndAttack());
            }
            else if (lungeTimer <= 0 && distanceToPlayer <= lungeDistance * 0.8f)
            {
                PerformLunge();
            }
            else
            {
                movementDirection = (playerTransform.position - transform.position).normalized;
                isMoving = true;
            }
        }
    }
    
    protected override void RevealedBehavior()
    {
        ChaseBehavior();
    }
    
    protected override void HiddenBehavior()
    {
        if (isDisguised)
        {
            isMoving = false;
            
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
                
                if (distanceToPlayer <= disguiseRange)
                {
                    StartCoroutine(RevealAndAttack());
                }
                else if (distanceToPlayer > disguiseRange * 3f && cooldownTimer <= 0 && disguiseTimer >= minDisguiseTime)
                {
                    StopChasingAndDisguise();
                }
            }
        }
        else
        {
            Wander();
            
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) > disguiseRange * 2f)
            {
                if (cooldownTimer <= 0 && disguiseTimer >= minDisguiseTime && Random.value < 0.01f)
                {
                    AssumeDisguise();
                }
            }
        }
    }
    
    protected override void OnPlayerContact(GameObject player)
    {
        if (isDisguised)
        {
            StartCoroutine(SurpriseAttack(player));
        }
        else if (isLunging)
        {
            DealDamageToPlayer(damageOnContact * surpriseAttackMultiplier);
            
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.Stun(lungeStunDuration);
            }
            
            EndLunge();
        }
        else if (canAttack)
        {
            DealDamageToPlayer(damageOnContact);
            StartCoroutine(AttackCooldown());
        }
    }
    
    private void UpdateDisguise()
    {
        if (isDisguised)
        {
            disguiseTimer += Time.deltaTime;
        }
    }
    
    private void UpdateLunge()
    {
        if (isLunging)
        {
            if (rb != null)
            {
                rb.velocity = lungeDirection * lungeSpeed;
            }
            
            float distanceTraveled = Vector2.Distance(lungeStartPosition, transform.position);
            if (distanceTraveled >= lungeDistance)
            {
                EndLunge();
            }
        }
    }
    
    private void UpdateItemAnimation()
    {
        if (!isDisguised || currentDisguise == null) return;
        
        currentDisguise.transform.Rotate(0, 0, itemRotationSpeed * Time.deltaTime);
        
        float newY = itemStartPosition.y + Mathf.Sin(Time.time * itemBobSpeed) * itemBobHeight;
        currentDisguise.transform.position = new Vector3(
            currentDisguise.transform.position.x,
            newY,
            currentDisguise.transform.position.z
        );
    }
    
    private void AssumeDisguise()
    {
        isDisguised = true;
        disguiseTimer = 0f;
        
        spriteRenderer.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        
        if (itemDisguises.Length > 0 && currentDisguise == null)
        {
            GameObject disguisePrefab = itemDisguises[Random.Range(0, itemDisguises.Length)];
            currentDisguise = Instantiate(disguisePrefab, transform.position, Quaternion.identity, transform);
            itemStartPosition = currentDisguise.transform.position;
        }
        
        SetMovementSpeed(moveSpeed * 0.3f);
        
        if (animator != null)
        {
            animator.SetBool("IsDisguised", true);
        }
    }
    
    private void DropDisguise()
    {
        isDisguised = false;
        cooldownTimer = disguiseCooldown;
        
        spriteRenderer.enabled = true;
        GetComponent<Collider2D>().enabled = true;
        
        if (currentDisguise != null)
        {
            Destroy(currentDisguise);
            currentDisguise = null;
        }
        
        SetMovementSpeed(moveSpeed);
        
        if (animator != null)
        {
            animator.SetBool("IsDisguised", false);
        }
    }
    
    private void StopChasingAndDisguise()
    {
        isMoving = false;
        AssumeDisguise();
    }
    
    private IEnumerator RevealAndAttack()
    {
        if (!isDisguised) yield break;
        
        DropDisguise();
        
        yield return new WaitForSeconds(revealDelay);
        
        Reveal();
        
        if (lungeTimer <= 0)
        {
            PerformLunge();
        }
    }
    
    private IEnumerator SurpriseAttack(GameObject player)
    {
        DropDisguise();
        
        yield return new WaitForSeconds(revealDelay);
        
        Reveal();
        
        DealDamageToPlayer(damageOnContact * surpriseAttackMultiplier);
        
        if (playerTransform != null)
        {
            Vector2 awayDirection = (transform.position - playerTransform.position).normalized;
            PerformLunge(awayDirection);
        }
    }
    
    private void PerformLunge(Vector2? direction = null)
    {
        if (playerTransform == null || isLunging) return;
        
        isLunging = true;
        lungeStartPosition = transform.position;
        lungeTimer = lungeCooldown;
        
        if (direction.HasValue)
        {
            lungeDirection = direction.Value;
        }
        else
        {
            lungeDirection = (playerTransform.position - transform.position).normalized;
        }
        
        if (animator != null)
        {
            animator.SetTrigger("Lunge");
        }
    }
    
    private void EndLunge()
    {
        isLunging = false;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        PlayerController player = playerTransform?.GetComponent<PlayerController>();
        if (player != null && Vector2.Distance(transform.position, playerTransform.position) < 1f)
        {
            player.Stun(lungeStunDuration * 0.5f);
        }
    }
    
    public override void TakeDamage(float damage)
    {
        if (isDisguised)
        {
            damage *= 1.5f;
            DropDisguise();
        }
        
        base.TakeDamage(damage);
    }
    
    public override void FlashlightHit(Vector3 sourcePosition)
    {
        if (isDisguised)
        {
            DropDisguise();
        }
        
        base.FlashlightHit(sourcePosition);
    }
    
    protected override void Die()
    {
        if (isDisguised)
        {
            DropDisguise();
        }
        
        base.Die();
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, disguiseRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lungeDistance);
    }
}