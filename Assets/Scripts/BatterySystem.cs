using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                OnChargeChanged?.Invoke(currentCharge / maxCharge);
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
    
    public bool IsRecharging => isRecharging;
    
    public event System.Action<float> OnChargeChanged;
    public event System.Action<bool> OnRechargeStateChanged;
    
    public BatterySystem(float initialMaxCharge, float initialCharge = -1)
    {
        MaxCharge = initialMaxCharge;
        CurrentCharge = initialCharge >= 0 ? initialCharge : initialMaxCharge;
    }
    
    public void Drain(float amount)
    {
        if (isRecharging) return;
        CurrentCharge -= Mathf.Abs(amount);
    }
    
    public void AddCharge(float amount)
    {
        CurrentCharge += Mathf.Abs(amount);
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
            CurrentCharge += rechargeRate * deltaTime;
        }
    }
    
    public bool HasCharge(float requiredCharge)
    {
        return currentCharge >= requiredCharge;
    }
}