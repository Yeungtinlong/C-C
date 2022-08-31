using System.Collections;
using System.Collections.Generic;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "IsChasingPointInRangeCondition", menuName = "State Machine/Conditions/Is Chasing Point In Range")]
public class IsChasingPointInRangeConditionSO : StateConditionSO
{
    protected override Condition CreateCondition() => new IsChasingPointInRangeCondition();
}

public class IsChasingPointInRangeCondition : Condition
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
                if (Vector3.Distance(_transform.position, command.Destination) <= _attacker.ChaseDistance)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
