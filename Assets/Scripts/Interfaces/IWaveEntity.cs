using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWaveEntity
{
    int WaveNumber { get; }
    void InitializeWave(int waveNumber);
    void OnWaveStart();
    void OnWaveEnd();
}