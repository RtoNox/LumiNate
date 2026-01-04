using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRechargeable
{
    float CurrentCharge { get; }
    float MaxCharge { get; }
    bool IsRecharging { get; }
    void StartRecharge(float rate);
    void StopRecharge();
    void Drain(float amount);
    void AddCharge(float amount);
    event System.Action<float> OnChargeChanged;
    event System.Action<bool> OnRechargeStateChanged;
}