using System.Collections;
using System.Collections.Generic;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;


[CreateAssetMenu(fileName = "EngineSmokeSwitcherAction", menuName = "State Machine/Actions/Engine Smoke Switcher")]
public class EngineSmokeSwitcherActionSO : StateActionSO
{
    public bool SwitcherOn;
    protected override StateAction CreateAction() => new EngineSmokeSwitcherAction();
}

public class EngineSmokeSwitcherAction : StateAction
{
    private VehicleEffectController _effectController;
    private EngineSmokeSwitcherActionSO _originSO => OriginSO as EngineSmokeSwitcherActionSO;

    public override void OnAwake(StateMachine stateMachine)
    {
        _effectController = stateMachine.GetComponent<VehicleEffectController>();
    }

    public override void OnStateEnter()
    {
        if (_originSO.SwitcherOn)
        {
            _effectController.PlayEngineOnEffect();
        }
        else
        {
            _effectController.StopEngineOnEffect();
        }
    }
}