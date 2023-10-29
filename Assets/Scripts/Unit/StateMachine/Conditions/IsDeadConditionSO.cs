using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsDeadCondition", menuName = "State Machine/Conditions/Is Dead")]
public class IsDeadConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsDeadCondition();
}

public class IsDeadCondition : Condition
{
    private Damageable _damageable = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _damageable = stateMachine.GetComponent<Damageable>();
    }

    public override bool Statement() => _damageable.IsDead && !_damageable.IsDestroyed;
}