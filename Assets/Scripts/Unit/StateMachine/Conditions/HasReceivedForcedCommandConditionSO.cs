using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "HasReceivedForcedCommandCondition", menuName = "State Machine/Conditions/Has Received Forced Command")]
public class HasReceivedForcedCommandConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new HasReceivedForcedCommandCondition();
}

public class HasReceivedForcedCommandCondition : Condition
{
    private Controllable _controllable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override bool Statement() => _controllable.IsReceivingForcedCommand;
}