using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "RemoveLayOrStandCommandAction", menuName = "State Machine/Actions/Remove Lay Or Stand Command")]
public class RemoveLayOrStandCommandActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new RemoveLayOrStandCommandAction();
}

public class RemoveLayOrStandCommandAction : StateAction
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
            if (command.CommandType == CommandType.LayOrStand)
                _controllable.RemoveCurrentCommand();
        }
    }
}