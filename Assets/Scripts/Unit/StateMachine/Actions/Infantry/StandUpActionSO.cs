using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "StandUpAction", menuName = "State Machine/Actions/Stand Up")]
public class StandUpActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new StandUpAction();
}

public class StandUpAction : StateAction
{
    private Human _human;

    public override void OnAwake(StateMachine stateMachine)
    {
        _human = stateMachine.GetComponent<Human>();
    }

    public override void OnStateExit()
    {
        _human.StandUp();
    }
}