using UnityEngine;
using System.Collections;

public class Crawler : BaseEnemy
{
    private PlayerController affectedPlayer;

    protected override void ChaseBehavior()
    {
        if (playerTransform != null)
        {
            movementDirection = (playerTransform.position - transform.position).normalized;
            isMoving = true;
            
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
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
}