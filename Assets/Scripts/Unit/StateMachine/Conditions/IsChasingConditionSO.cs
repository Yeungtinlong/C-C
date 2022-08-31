using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsChasingCondition", menuName = "State Machine/Conditions/Is Chasing")]
public class IsChasingConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsChasingCondition();
}

public class IsChasingCondition : Condition
{
    private Controllable _controllable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Chase)
            {
                return true;
            }
        }

        return false;
    }
}
