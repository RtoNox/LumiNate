using UnityEngine;
using System.Collections;

public class Dasher : BaseEnemy
{
    [Header("Dasher Specific Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 4f;
    [SerializeField] private float dashWindupTime = 0.5f;
    [SerializeField] private float dashDamage = 20f;
    [SerializeField] private float dashKnockback = 15f;
    [SerializeField] private int maxDashes = 2;
    
    [Header("Evasion")]
    [SerializeField] private float evasionChance = 0.3f;
    [SerializeField] private float evasionSpeed = 10f;
    [SerializeField] private float evasionDuration = 0.5f;
    
    private float dashTimer = 0f;
    private float windupTimer = 0f;
    private float evasionTimer = 0f;
    private int dashesRemaining;
    private bool isDashing = false;
    private bool isWindingUp = false;
    private bool isEvading = false;
    private Vector2 dashDirection;
    
    protected override void Start()
    {
        base.Start();
        
        maxHealth *= 0.4f;
        currentHealth = maxHealth;
        moveSpeed *= 2f;
        chaseSpeed *= 2.5f;
        
        dashesRemaining = maxDashes;
        dashTimer = Random.Range(0f, 2f);
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (IsDead || !isActive) return;
        
        UpdateDash();
        UpdateEvasion();
    }
    
    protected override void ChaseBehavior()
    {
        if (isDashing || isWindingUp || isEvading) return;
        
        if (playerTransform != null)
        {
            movementDirection = (playerTransform.position - transform.position).normalized;
            isMoving = true;
            
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (dashTimer <= 0 && dashesRemaining > 0 && distanceToPlayer > 2f)
            {
                StartDashWindup();
            }
            
            if (playerController != null && playerController.IsFlashlightOn && 
                IsInFlashlight() && Random.value < evasionChance * Time.deltaTime)
            {
                StartEvasion();
            }
        }
    }
    
    protected override void RevealedBehavior()
    {
        ChaseBehavior();
        
        if (isRevealed && !isDashing && !isWindingUp && dashTimer <= 0 && dashesRemaining > 0)
        {
            if (Random.value < 0.1f * Time.deltaTime)
            {
                StartDashWindup();
            }
        }
    }
    
    protected override void HiddenBehavior()
    {
        if (Random.value < 0.02f)
        {
            movementDirection = Random.insideUnitCircle.normalized;
            isMoving = true;
        }
        else if (Random.value < 0.01f)
        {
            isMoving = false;
        }
        
        if (Random.value < 0.005f && playerTransform != null && IsPlayerInChaseRange)
        {
            Vector2 randomOffset = Random.insideUnitCircle * 3f;
            Vector2 newPosition = (Vector2)playerTransform.position + randomOffset;
            
            StartCoroutine(QuickTeleport(newPosition));
        }
    }
    
    protected override void OnPlayerContact(GameObject player)
    {
        if (isDashing)
        {
            DealDamageToPlayer(dashDamage);
            
            PlayerController playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                playerCtrl.Stun(0.5f);
                playerCtrl.ApplyKnockback(dashDirection, dashKnockback);
            }
            
            EndDash();
        }
        else if (canAttack)
        {
            DealDamageToPlayer(damageOnContact);
            StartCoroutine(AttackCooldown());
        }
    }
    
    private void UpdateDash()
    {
        if (dashTimer > 0) dashTimer -= Time.deltaTime;
        
        if (isWindingUp)
        {
            windupTimer -= Time.deltaTime;
            
            spriteRenderer.color = Color.Lerp(revealedColor, Color.blue, Mathf.PingPong(Time.time * 10f, 1f));
            
            if (rb != null)
                rb.velocity *= 0.8f;
            
            if (windupTimer <= 0)
            {
                ExecuteDash();
            }
        }
        else if (isDashing)
        {
            PerformDash();
        }
    }
    
    private void UpdateEvasion()
    {
        if (isEvading)
        {
            evasionTimer -= Time.deltaTime;
            
            if (evasionTimer <= 0)
            {
                EndEvasion();
            }
        }
    }
    
    private void StartDashWindup()
    {
        if (playerTransform == null) return;
        
        isWindingUp = true;
        windupTimer = dashWindupTime;
        isMoving = false;
        
        Vector2 playerVelocity = Vector2.zero;
        if (playerController != null && playerController.Rigidbody != null)
        {
            playerVelocity = playerController.Rigidbody.velocity;
        }
        
        Vector2 predictedPosition = (Vector2)playerTransform.position + playerVelocity * 0.3f;
        dashDirection = (predictedPosition - (Vector2)transform.position).normalized;
        
        if (animator != null)
        {
            animator.SetTrigger("WindupDash");
        }
    }
    
    private void ExecuteDash()
    {
        isWindingUp = false;
        isDashing = true;
        dashesRemaining--;
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        if (animator != null)
        {
            animator.SetBool("IsDashing", true);
        }
        
        Invoke(nameof(EndDash), dashDuration);
    }
    
    private void PerformDash()
    {
        if (rb != null)
        {
            rb.velocity = dashDirection * dashSpeed;
        }
    }
    
    private void EndDash()
    {
        if (!isDashing) return;
        
        isDashing = false;
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        if (animator != null)
        {
            animator.SetBool("IsDashing", false);
        }
        
        if (dashesRemaining <= 0)
        {
            dashTimer = dashCooldown;
            dashesRemaining = maxDashes;
        }
        
        dashTimer = 1f;
    }
    
    private void StartEvasion()
    {
        if (isEvading || isDashing || isWindingUp) return;
        
        isEvading = true;
        evasionTimer = evasionDuration;
        
        Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
        Vector2 evadeDirection = new Vector2(-toPlayer.y, toPlayer.x);
        
        if (Random.value > 0.5f)
        {
            evadeDirection = -evadeDirection;
        }
        
        movementDirection = evadeDirection;
        
        if (animator != null)
        {
            animator.SetTrigger("Evade");
        }
        
        if (rb != null)
        {
            rb.AddForce(evadeDirection * evasionSpeed, ForceMode2D.Impulse);
        }
    }
    
    private void EndEvasion()
    {
        isEvading = false;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    private IEnumerator QuickTeleport(Vector2 targetPosition)
    {
        Vector2 originalPosition = transform.position;
        Color originalColor = spriteRenderer.color;
        
        float fadeTime = 0.1f;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            float alpha = 1 - (t / fadeTime);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        transform.position = targetPosition;
        
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            float alpha = t / fadeTime;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
    }
    
    private bool IsInFlashlight()
    {
        if (playerController == null || playerController.flashlight == null) return false;
        
        Flashlight flashlight = playerController.flashlight;
        var enemiesInLight = flashlight.GetEnemiesInLight();
        
        return enemiesInLight.Contains(this);
    }
    
    public override void TakeDamage(float damage)
    {
        if (isDashing)
        {
            damage *= 1.5f;
        }
        
        base.TakeDamage(damage);
        
        if (!IsDead && !isDashing && !isWindingUp && !isEvading)
        {
            if (Random.value < 0.5f)
            {
                StartEvasion();
            }
        }
    }
    
    public override void FlashlightHit(Vector3 sourcePosition)
    {
        if (!isEvading && !isDashing && !isWindingUp && Random.value < 0.7f)
        {
            StartEvasion();
        }
        
        base.FlashlightHit(sourcePosition);
    }
}