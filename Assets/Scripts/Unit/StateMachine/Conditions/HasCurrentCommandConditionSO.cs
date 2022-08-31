using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "HasCurrentCommandCondition", menuName = "State Machine/Conditions/Has Current Command")]
public class HasCurrentCommandConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new HasCurrentCommandCondition();
}

public class HasCurrentCommandCondition : Condition
{
    private Controllable _controllable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override bool Statement() => _controllable.TryGetCurrentCommand(out Command command);
}