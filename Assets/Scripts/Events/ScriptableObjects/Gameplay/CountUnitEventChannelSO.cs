using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "CountUnitEventChannel", menuName = "Events/Gameplay/Count Unit Event Channel")]
public class CountUnitEventChannelSO : ScriptableObject
{
    public Func<Damageable, int> OnEventRaised;

    public int RaiseEvent(Damageable damageable)
    {
        if (OnEventRaised != null)
        {
            return OnEventRaised.Invoke(damageable);
        }
        
        return 0; 
    }
}
