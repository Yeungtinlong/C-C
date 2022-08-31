using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsTimeToStandCondition", menuName = "State Machine/Conditions/Is Time To Stand")]
public class IsTimeToStandConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsTimeToStandCondition();
}

public class IsTimeToStandCondition : Condition
{
    private Human _human;

    public override void OnAwake(StateMachine stateMachine)
    {
        _human = stateMachine.GetComponent<Human>();
    }

    public override bool Statement() => _human.IsTimeToAutomaticallyStand;
}