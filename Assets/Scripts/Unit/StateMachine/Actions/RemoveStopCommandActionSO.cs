using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveStopCommandAction_OnExit", menuName = "State Machine/Actions/Remove Stop Command OnExit")]
public class RemoveStopCommandActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new RemoveStopCommandAction();
}

public class RemoveStopCommandAction : StateAction
{
    private Controllable _controllable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override void OnStateExit()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Stop)
                _controllable.RemoveCurrentCommand();
        }
    }
}