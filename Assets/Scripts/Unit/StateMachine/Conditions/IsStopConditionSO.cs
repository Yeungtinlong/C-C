using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsStopCondition", menuName = "State Machine/Conditions/Is Stop")]
public class IsStopConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsStopCondition();
}

public class IsStopCondition : Condition
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
            if (command.CommandType == CommandType.Stop)
                return true;
        }
            
        return false;
    }
}