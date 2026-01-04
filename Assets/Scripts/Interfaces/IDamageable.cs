using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    void TakeDamage(float damage);
    void Heal(float amount);
    bool IsDead { get; }
    event System.Action OnDeath;
    event System.Action<float> OnHealthChanged;
}