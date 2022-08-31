using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsChasingTargetInRangeCondition", menuName = "State Machine/Conditions/Is Chasing Target In Range")]
public class IsChasingTargetInRangeConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsChasingTargetInRangeCondition();
}

public class IsChasingTargetInRangeCondition : Condition
{
    private Transform _transform = default;
    private Attacker _attacker = default;
    private Controllable _controllable = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _transform = stateMachine.transform;
        _attacker = stateMachine.GetComponent<Attacker>();
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Chase)
            {
                if (command.Target == null)
                    return false;

                if (Vector3.Distance(_transform.position, command.Target.transform.position) <= _attacker.ChaseDistance)
                {
                    _attacker.SetCurrentEnemy(command.Target);
                    return true;
                }
                else
                {
                    _attacker.RemoveCurrentEnemy();
                    return false;
                }

            }
        }

        return false;
    }
}
