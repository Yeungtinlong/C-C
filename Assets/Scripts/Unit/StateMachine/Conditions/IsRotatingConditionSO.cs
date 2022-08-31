using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IsRotatingCondition", menuName = "State Machine/Conditions/Is Rotating")]
public class IsRotatingConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsRotatingCondition();
}

public class IsRotatingCondition : Condition
{
    private Controllable _controllable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
            return command.CommandType == CommandType.Rotate;

        return false;
    }
}