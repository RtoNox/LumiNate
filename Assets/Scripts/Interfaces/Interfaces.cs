public interface IDamageable
{
    void TakeDamage(float damage);
    void Heal(float amount);
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }
    
    event System.Action<float> OnDamageTaken;
    event System.Action<float> OnHealed;
    event System.Action OnDeath;
}

public interface IRechargeable
{
    float CurrentCharge { get; }
    float MaxCharge { get; }
    bool IsFullyCharged { get; }
    
    void Recharge(float amount);
    void ConsumeCharge(float amount);
    void SetMaxCharge(float newMax);
    
    event System.Action<float> OnChargeChanged;
    event System.Action<float> OnChargeConsumed;
    event System.Action OnFullyCharged;
}

public interface IRevealable
{
    bool IsRevealed { get; }
    float RevealDuration { get; }
    float RevealTimer { get; }
    
    void Reveal();
    void Hide();
    void SetRevealDuration(float duration);
    
    event System.Action OnRevealed;
    event System.Action OnHidden;
}

public interface IWaveEntity
{
    int WaveNumber { get; }
    bool IsActive { get; }
    bool IsCompleted { get; }
    
    void InitializeWave(int waveNumber);
    void StartWave();
    void CompleteWave();
    void FailWave();
    
    event System.Action<int> OnWaveStarted;
    event System.Action<int> OnWaveCompleted;
    event System.Action<int> OnWaveFailed;
}