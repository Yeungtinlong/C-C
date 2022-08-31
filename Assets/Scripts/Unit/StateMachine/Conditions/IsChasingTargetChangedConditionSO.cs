using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsChasingTargetChangedCondition", menuName = "State Machine/Conditions/Is Chasing Target Changed")]
public class IsChasingTargetChangedConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsChasingTargetChangedCondition();
}

public class IsChasingTargetChangedCondition : Condition
{
    private Controllable _controllable = default;
    private Attacker _attacker = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
        _attacker = stateMachine.GetComponent<Attacker>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Chase)
            {
                if (command.Target != _attacker.CurrentEnemy)
                {
                    _attacker.SetCurrentEnemy(null);
                    return true;
                }
            }
        }

        return false;
    }
}
