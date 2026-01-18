using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class BatterySystem : IRechargeable
{
    private float currentCharge;
    private float maxCharge;
    private bool isRecharging = false;
    private float rechargeRate = 0f;
    
    public float CurrentCharge 
    { 
        get => currentCharge;
        private set
        {
            float oldValue = currentCharge;
            currentCharge = Mathf.Clamp(value, 0, maxCharge);
            if (Mathf.Abs(oldValue - currentCharge) > 0.01f)
            {
                OnChargeChanged?.Invoke(currentCharge);
            }
            
            if (currentCharge >= maxCharge && oldValue < maxCharge)
            {
                OnFullyCharged?.Invoke();
            }
        }
    }
    
    public float MaxCharge 
    { 
        get => maxCharge; 
        set
        {
            maxCharge = Mathf.Max(1, value);
            CurrentCharge = Mathf.Min(currentCharge, maxCharge);
        }
    }
    
    public bool IsFullyCharged => currentCharge >= maxCharge;
    
    public bool IsRecharging => isRecharging;
    
    public event Action<float> OnChargeChanged;
    public event Action<float> OnChargeConsumed;
    public event Action OnFullyCharged;
    
    public event Action<bool> OnRechargeStateChanged;
    
    public BatterySystem(float initialMaxCharge, float initialCharge = -1)
    {
        MaxCharge = initialMaxCharge;
        CurrentCharge = initialCharge >= 0 ? initialCharge : initialMaxCharge;
    }
    
    public void Recharge(float amount)
    {
        if (amount <= 0 || IsFullyCharged) return;
        
        float oldCharge = currentCharge;
        CurrentCharge += amount;
        
        OnChargeChanged?.Invoke(currentCharge);
        
        if (IsFullyCharged)
        {
            OnFullyCharged?.Invoke();
        }
    }
    
    public void ConsumeCharge(float amount)
    {
        if (amount <= 0) return;
        
        float oldCharge = currentCharge;
        CurrentCharge -= amount;
        float actualConsumed = Mathf.Max(0, oldCharge - currentCharge);
        
        OnChargeChanged?.Invoke(currentCharge);
        if (actualConsumed > 0)
        {
            OnChargeConsumed?.Invoke(actualConsumed);
        }
    }
    
    public void SetMaxCharge(float newMax)
    {
        if (newMax <= 0) return;
        
        float percentage = currentCharge / maxCharge;
        MaxCharge = newMax;
        CurrentCharge = maxCharge * percentage;
        
        OnChargeChanged?.Invoke(currentCharge);
    }
    
    public void Drain(float amount)
    {
        if (isRecharging) return;
        ConsumeCharge(amount);
    }
    
    public void AddCharge(float amount)
    {
        Recharge(amount);
    }
    
    public void StartRecharge(float rate)
    {
        if (rate <= 0 || CurrentCharge >= MaxCharge) return;
        
        isRecharging = true;
        rechargeRate = rate;
        OnRechargeStateChanged?.Invoke(true);
    }
    
    public void StopRecharge()
    {
        if (!isRecharging) return;
        
        isRecharging = false;
        rechargeRate = 0f;
        OnRechargeStateChanged?.Invoke(false);
    }
    
    public void Update(float deltaTime)
    {
        if (isRecharging && rechargeRate > 0)
        {
            Recharge(rechargeRate * deltaTime);
        }
    }
    
    public bool HasCharge(float requiredCharge)
    {
        return currentCharge >= requiredCharge;
    }
    
    public float ChargePercentage => currentCharge / maxCharge;
}