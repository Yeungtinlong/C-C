using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "FallDownAction", menuName = "State Machine/Actions/Fall Down")]
public class FallDownActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new FallDownAction();
}

public class FallDownAction : StateAction
{
    private Human _human;

    public override void OnAwake(StateMachine stateMachine)
    {
        _human = stateMachine.GetComponent<Human>();
    }

    public override void OnStateEnter()
    {
        _human.LayByGettingHit();
    }
}