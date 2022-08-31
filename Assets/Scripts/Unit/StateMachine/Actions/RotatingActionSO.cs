using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "RotatingAction", menuName = "State Machine/Actions/Rotating")]
public class RotatingActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new RotatingAction();
}

public class RotatingAction : StateAction
{
    private PathDriver _driver;
    private Controllable _controllable;
    private Transform _transform;

    public override void OnAwake(StateMachine stateMachine)
    {
        _driver = stateMachine.GetComponent<PathDriver>();
        _controllable = stateMachine.GetComponent<Controllable>();
        _transform = stateMachine.transform;
    }

    public override void OnUpdate()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Rotate)
            {
                _driver.SetInPlaceTurn(command.TargetAlignment);
            }
            // else
            // {
            //     Command jumpQueueRotate = new Command
            //     {
            //         CommandType = CommandType.Rotate,
            //         TargetAlignment = (command.Destination - _transform.position).normalized
            //     };
            //     _controllable.AddCommand(jumpQueueRotate, false, true);
            //
            //     // _driver.SetInPlaceTurn(jumpQueueRotate.TargetAlignment);
            // }
        }
    }
}