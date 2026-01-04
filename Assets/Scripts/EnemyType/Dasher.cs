using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dasher : BaseEnemy
{
    [Header("Dasher Settings")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float dashDuration = 0.5f;
    
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    
    // Override InitializeEnemyType
    protected override void InitializeEnemyType()
    {
        enemyName = "Dasher";
        baseSpeed = 4f;
        baseHealth = 50f;
        contactDamage = 20f;
        currentSpeed = baseSpeed;
    }
    
    // Override Update method
    protected override void Update()
    {
        // Call parent Update for base functionality
        base.Update();
        
        // Add dasher-specific update logic
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
                EndDash();
        }
        
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;
    }
    
    // Override UpdateMovement
    protected override void UpdateMovement()
    {
        if (isDashing) return;
        
        MoveTowardPlayer();
        
        // Try to dash
        if (cooldownTimer <= 0 && Vector3.Distance(transform.position, player.position) < 8f)
        {
            StartDash();
        }
    }
    
    // Override UpdateSpecialAbility
    protected override void UpdateSpecialAbility()
    {
        // Dash is the special ability
        // Logic handled in UpdateMovement
    }
    
    // Dasher-specific methods
    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        currentSpeed = dashSpeed;
        cooldownTimer = dashCooldown;
        Debug.Log($"{enemyName} started dashing!");
    }
    
    void EndDash()
    {
        isDashing = false;
        currentSpeed = baseSpeed;
        Debug.Log($"{enemyName} ended dash");
    }
    
    // Override ScaleWithWave
    protected override void ScaleWithWave(int waveNum)
    {
        base.ScaleWithWave(waveNum);
        dashCooldown = Mathf.Max(1f, dashCooldown - (waveNum * 0.1f));
    }
    
    // Override OnAttackPlayer for dash-specific attack
    protected override void OnAttackPlayer()
    {
        if (isDashing)
        {
            Debug.Log($"{enemyName} delivered a powerful dash attack!");
        }
        else
        {
            base.OnAttackPlayer();
        }
    }
}