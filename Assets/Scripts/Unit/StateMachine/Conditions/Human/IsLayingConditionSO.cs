using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsLayingCondition", menuName = "State Machine/Conditions/Is Laying")]
public class IsLayingConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsLayingCondition();
}

public class IsLayingCondition : Condition
{
    private Human _human;

    public override void OnAwake(StateMachine stateMachine)
    {
        _human = stateMachine.GetComponent<Human>();
    }

    public override bool Statement() => _human.IsLaying;
}