using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleEffectController : UnitEffectController
{
    [SerializeField] private ParticleSystem _lowHealthEffect = default;
    [SerializeField] private ParticleSystem _engineOnEffect = default;
    [SerializeField] private VehicleBurstEffect _burstEffect = default;

    public void PlayLowHealthEffect()
    {
        if (!_lowHealthEffect.isPlaying)
        {
            _lowHealthEffect.Play();
        }
    }
    
    public void StopLowHealthEffect()
    {
        if (_lowHealthEffect.isPlaying)
        {
            _lowHealthEffect.Stop();
        }
    }
    
    public void PlayEngineOnEffect()
    {
        if (!_engineOnEffect.isPlaying)
        {
            _engineOnEffect.Play();
        }
    }

    public void StopEngineOnEffect()
    {
        if (_engineOnEffect.isPlaying)
        {
            _engineOnEffect.Stop();
        }
    }

    public override void PlayDieEffect()
    {
        _burstEffect.Play();
        StopLowHealthEffect();
        StopEngineOnEffect();
    }
}