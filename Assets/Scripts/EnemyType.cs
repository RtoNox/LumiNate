using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Crawler,    // Minute 0-5: Basic walking enemy
    Dasher,     // Minute 5-10: Fast but low HP
    Mimic,      // Minute 10-15: Disguises as items
    Drainer,    // Minute 15-20: Drains flashlight battery
}