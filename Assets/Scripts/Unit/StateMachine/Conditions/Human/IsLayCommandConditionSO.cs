using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsLayCommandCondition", menuName = "State Machine/Conditions/Is Lay Command")]
public class IsLayCommandConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsLayCommandCondition();
}

public class IsLayCommandCondition : Condition
{
    private Controllable _controllable;
    private Human _human;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
        _human = stateMachine.GetComponent<Human>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.LayOrStand && !_human.IsLaying)
            {
                return true;
            }
        }

        return false;
    }
}