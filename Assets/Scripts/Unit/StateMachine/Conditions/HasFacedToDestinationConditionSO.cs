using UnityEngine;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "HasFacedToDestinationCondition", menuName = "State Machine/Conditions/Has Faced To Destination")]
public class HasFacedToDestinationConditionSO : StateConditionSO
{
    [SerializeField] private float _threshold = default;
    public float Threshold => _threshold;
    protected override Condition CreateCondition() => new HasFacedToDestinationCondition();
}

public class HasFacedToDestinationCondition : Condition
{
    private Attacker _attacker;
    private Transform _transform;
    private Controllable _controllable;

    private HasFacedToDestinationConditionSO _originSO => OriginSO as HasFacedToDestinationConditionSO;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
        _transform = stateMachine.transform;
        _controllable = stateMachine.GetComponent<Controllable>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Move)
            {
                Vector3 dir = (command.Destination - _transform.position).normalized;
                return Vector3.Angle(_transform.forward, dir) <= _originSO.Threshold;
            }
        }

        return false;
    }
}