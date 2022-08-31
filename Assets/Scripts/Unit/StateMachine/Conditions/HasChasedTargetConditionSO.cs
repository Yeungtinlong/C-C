using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "HasChasedTargetCondition", menuName = "State Machine/Conditions/Has Chased Target")]
public class HasChasedTargetConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new HasChasedTargetCondition();
}

public class HasChasedTargetCondition : Condition
{
    private Transform _transform = default;
    private Controllable _controllable = default;
    private Attacker _attacker = default;
    private PathDriver _driver = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _transform = stateMachine.transform;
        _controllable = stateMachine.GetComponent<Controllable>();
        _attacker = stateMachine.GetComponent<Attacker>();
        _driver = stateMachine.GetComponent<PathDriver>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Chase)
            {
                if (command.Target != null && _driver.IsArrived && !_driver.IsNewApproachRange(_transform.position, command.ApproachRange))
                {
                    _attacker.SetCurrentEnemy(command.Target);
                    return true;
                }
            }
        }

        return false;
    }
}
