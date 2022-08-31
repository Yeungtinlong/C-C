using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "ResetReceviedCommandAction", menuName = "State Machine/Actions/Reset Recevied Command")]
public class ResetReceviedCommandActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new ResetReceviedCommandAction();
}

public class ResetReceviedCommandAction : StateAction
{
    private Controllable _controllable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override void OnStateEnter()
    {
        _controllable.IsReceivingForcedCommand = false;
    }
}