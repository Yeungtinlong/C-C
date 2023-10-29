using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "HasRotatedToOrientationCondition", menuName = "State Machine/Conditions/Has Rotated To Orientation")]
public class HasRotatedToOrientationConditionSO : StateConditionSO
{
    [SerializeField] internal float _threshold = default;
    public float Threshold => _threshold;
    protected override Condition CreateCondition() => new HasRotatedToOrientationCondition();
}

public class HasRotatedToOrientationCondition : Condition
{
    private HasRotatedToOrientationConditionSO _originSO => OriginSO as HasRotatedToOrientationConditionSO;

    private Transform _transform;
    private Controllable _controllable;
    private IPathDriver _driver;

    public override void OnAwake(StateMachine stateMachine)
    {
        _transform = stateMachine.transform;
        _controllable = stateMachine.GetComponent<Controllable>();
        _driver = stateMachine.GetComponent<IPathDriver>();
    }

    public override bool Statement()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
            if (command.CommandType == CommandType.Rotate)
            {
                if (_driver.IsArrived && !_driver.IsNewAlignment(command.TargetAlignment))
                {
                    _controllable.RemoveCurrentCommand();
                    return true;
                }
            }

        return false;
    }
}