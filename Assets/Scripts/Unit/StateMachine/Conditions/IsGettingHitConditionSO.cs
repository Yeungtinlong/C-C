using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsGettingHitCondition", menuName = "State Machine/Conditions/Is Getting Hit")]
public class IsGettingHitConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsGettingHitCondition();
}

public class IsGettingHitCondition : Condition
{
    private Damageable _damageable = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _damageable = stateMachine.GetComponent<Damageable>();
    }

    public override bool Statement() => _damageable.IsGettingHit;
}