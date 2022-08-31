using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerAimAtChasingTargetAction", menuName = "State Machine/Actions/Tower Aim At Chasing Target")]
public class TowerAimAtChasingTargetActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new TowerAimAtChasingTargetAction();
}

public class TowerAimAtChasingTargetAction : StateAction
{
    private RotatableTower _rotatableTower = default;
    private Controllable _controllable = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _rotatableTower = stateMachine.GetComponent<RotatableTower>();
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override void OnUpdate()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Chase)
            {
                if (command.Target != null)
                {
                    _rotatableTower.RotateTo(Vector3.Normalize(command.Target.transform.position - _rotatableTower.TowerAnchor.position));
                }
            }
        }
    }
}