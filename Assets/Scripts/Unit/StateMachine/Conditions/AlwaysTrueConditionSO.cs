using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "AlwaysTrueCondition", menuName = "State Machine/Conditions/Always True")]
public class AlwaysTrueConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new AlwaysTrueCondition();
}

public class AlwaysTrueCondition : Condition
{
    public override bool Statement() => true;
}