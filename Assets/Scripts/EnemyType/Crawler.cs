using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crawler : BaseEnemy
{
    protected override void InitializeEnemyType()
    {
        enemyName = "Crawler";
        baseSpeed = 2f;
        baseHealth = 150f;
        contactDamage = 15f;
    }
    
    protected override void UpdateMovement()
    {
        MoveTowardPlayer();
    }
}