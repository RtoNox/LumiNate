using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drainer : BaseEnemy
{
    [Header("Drainer Settings")]
    [SerializeField] private float drainRadius = 3f;
    [SerializeField] private float drainRate = 5f;
    
    protected override void InitializeEnemyType()
    {
        enemyName = "Drainer";
        baseSpeed = 1.5f;
        baseHealth = 120f;
        contactDamage = 5f;
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (player != null && Vector3.Distance(transform.position, player.position) < drainRadius)
        {
            DrainBattery();
        }
    }
    
    protected override void UpdateMovement()
    {
        MoveTowardPlayer();
    }
    
    protected override void UpdateSpecialAbility()
    {
        //don in DrainBattery()
    }
    
    void DrainBattery()
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.Battery.Drain(drainRate * Time.deltaTime);
        }
    }
    
    protected override void OnAttackPlayer()
    {
        base.OnAttackPlayer();
        Debug.Log($"{enemyName} drained player's energy!");
    }
}