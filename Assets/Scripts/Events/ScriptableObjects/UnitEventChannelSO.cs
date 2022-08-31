using UnityEngine;
using UnityEngine.Events;
using CNC.StateMachine;

[CreateAssetMenu(menuName = "Events/Unit Event Channel")]
public class UnitEventChannelSO : EventChannelBaseSO
{
    public UnityAction<Damageable> OnEventRaised;

    public void RaiseEvent(Damageable unit)
    {
        if (OnEventRaised != null)
        {
            OnEventRaised.Invoke(unit);
        }
    }
}
