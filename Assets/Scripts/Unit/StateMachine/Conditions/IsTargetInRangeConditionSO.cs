using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsTargetInRangeCondition", menuName = "State Machine/Conditions/Is Target In Range")]
public class IsTargetInRangeConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsTargetInRangeCondition();
}

public class IsTargetInRangeCondition : Condition
{
    private Attacker _attacker = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
    }

    public override bool Statement()
    {
        if (!_attacker.IsTargetInRange())
        {
            _attacker.SetCurrentEnemy(null);
            return false;
        }

        return true;
    }
}