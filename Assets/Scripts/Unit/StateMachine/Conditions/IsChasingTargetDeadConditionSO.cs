using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsChasingTargetDeadCondition", menuName = "State Machine/Conditions/Is Chasing Target Dead")]
public class IsChasingTargetDeadConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsChasingTargetDeadCondition();
}

public class IsChasingTargetDeadCondition : Condition
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
                if (command.Target == null || command.Target.IsDead)
                {
                    _controllable.RemoveCurrentCommand();
                    return true;
                }
            }
        }

        return false;
    }
}