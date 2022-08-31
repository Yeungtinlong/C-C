using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "HasReachedCondition", menuName = "State Machine/Conditions/Has Reached")]
public class HasReachedConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new HasReachedCondition();
}

public class HasReachedCondition : Condition
{
    private Controllable _controllable;
    private PathDriver _driver;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
        _driver = stateMachine.GetComponent<PathDriver>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Move)
            {
                if (_driver.IsArrived && !_driver.IsNewDestination(command.Destination))
                {
                    _controllable.RemoveCurrentCommand();
                    return true;
                }
            }
        }

        return false;
    }
}