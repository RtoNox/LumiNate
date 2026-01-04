using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mimic : BaseEnemy
{
    [Header("Mimic Settings")]
    [SerializeField] private GameObject itemModel;
    [SerializeField] private float revealDistance = 2f;
    
    private bool isDisguised = true;
    
    protected override void InitializeEnemyType()
    {
        enemyName = "Mimic";
        baseSpeed = 1.5f;
        baseHealth = 80f;
        contactDamage = 25f;
        
        DisguiseAsItem();
    }
    
    protected override void Update()
    {
        if (!isDisguised)
        {
            base.Update();
        }
        else
        {
            if (player != null && Vector3.Distance(transform.position, player.position) < revealDistance)
            {
                RevealMimic();
            }
        }
    }
    
    protected override void UpdateMovement()
    {
        if (isDisguised) return;
        MoveTowardPlayer();
    }
    
    protected override void UpdateSpecialAbility()
    {
        //don in Update()
    }
    
    public override void OnFlashlightHit(bool isKillMode)
    {
        if (isDisguised)
            RevealMimic();
        
        base.OnFlashlightHit(isKillMode);
    }
    
    void DisguiseAsItem()
    {
        isDisguised = true;
        if (itemModel != null)
        {
            itemModel.SetActive(true);
        }
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
            if (col is SphereCollider sphere)
                sphere.radius = 0.3f;
        }
        gameObject.tag = "Collectible";
        
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.yellow;
        }
    }
    
    void RevealMimic()
    {
        isDisguised = false;
        if (itemModel != null)
            itemModel.SetActive(false);
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
            if (col is SphereCollider sphere)
                sphere.radius = 0.5f;
        }
        gameObject.tag = "Enemy";
        
        if (enemyRenderer != null && invisibleMaterial != null)
        {
            enemyRenderer.material = invisibleMaterial;
        }
        
        Reveal(5f);
        currentSpeed = baseSpeed * 2f;
        Debug.Log("Mimic revealed!");
    }
    
    public override void Reveal(float duration)
    {
        if (isDisguised)
            RevealMimic();
        
        base.Reveal(duration);
    }
}