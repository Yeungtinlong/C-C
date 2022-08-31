using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CNC.StateMachine;

[CreateAssetMenu(menuName = "Events/Projectile Event Channel")]
public class ProjectileEventChannelSO : EventChannelBaseSO
{
    public UnityAction<Projectile, Transform, Damageable, Attacker> OnEventRaised;

    public void RaiseEvent(Projectile projectile, Transform launcher, Damageable target, Attacker owner)
    {
        if (OnEventRaised != null)
        {
            OnEventRaised.Invoke(projectile, launcher, target, owner);
        }
    }
}