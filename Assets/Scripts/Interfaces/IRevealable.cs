using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRevealable
{
    bool IsVisible { get; }
    void Reveal(float duration);
    void Hide();
    void OnFlashlightHit(bool isKillMode);
}
