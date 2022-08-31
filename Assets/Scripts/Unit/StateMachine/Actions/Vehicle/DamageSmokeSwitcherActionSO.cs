using UnityEngine;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "DamageSmokeSwitcherAction", menuName = "State Machine/Actions/Damage Smoke Switcher")]
public class DamageSmokeSwitcherActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new DamageSmokeSwitcherAction();
}

public class DamageSmokeSwitcherAction : StateAction
{
    private VehicleEffectController _effectController;
    private Damageable _damageable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _effectController = stateMachine.GetComponent<VehicleEffectController>();
        _damageable = stateMachine.GetComponent<Damageable>();
    }

    public override void OnStateExit()
    {
        if (_damageable.IsLowHealth)
        {
            _effectController.PlayLowHealthEffect();
        }
        else
        {
            _effectController.StopLowHealthEffect();
        }
    }
}
