using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsTargetDead", menuName = "State Machine/Conditions/Is Target Dead")]
public class IsTargetDeadConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsTargetDeadCondition();
}

public class IsTargetDeadCondition : Condition
{
    private Attacker _attacker = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
    }

    public override bool Statement()
    {
        return _attacker.CurrentEnemy == null || _attacker.CurrentEnemy.IsDead;
    }
}
