using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "StopAction", menuName = "State Machine/Actions/Stop")]
public class StopActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new StopAction();
}

public class StopAction : StateAction
{
    private PathDriver _driver = default;
    private Controllable _controllable = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _driver = stateMachine.GetComponent<PathDriver>();
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override void OnStateEnter()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Stop)
                _driver.Stop(false);
            
            _controllable.RemoveCurrentCommand();
        }
    }

    public override void OnUpdate()
    {
        
    }
}