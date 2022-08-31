using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsMovingCondition", menuName = "State Machine/Conditions/Is Moving")]
public class IsMovingConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsMovingCondition();
}

public class IsMovingCondition : Condition
{
    private Controllable _controllable = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Move)
                return true;
        }
            
        return false;
    }
}