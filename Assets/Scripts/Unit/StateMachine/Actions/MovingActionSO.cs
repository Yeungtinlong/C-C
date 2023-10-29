using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "MovingAction", menuName = "State Machine/Actions/Moving")]
public class MovingActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new MovingAction();
}

public class MovingAction : StateAction
{
    private IPathDriver _driver;
    private Controllable _controllable;
    private Animator _animator;

    public override void OnAwake(StateMachine stateMachine)
    {
        _driver = stateMachine.GetComponent<IPathDriver>();
        _controllable = stateMachine.GetComponent<Controllable>();
        _animator = stateMachine.TryGetComponent(out _animator)
            ? _animator
            : stateMachine.GetComponent<AnimatorController>().ManualAnimator;
    }

    public override void OnUpdate()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Move)
            {
                _driver.SetDestination(command.Destination, command.TargetAlignment, command.IsForcedAlign, 0f);
                _animator.speed = _driver.CurrentSpeed / _driver.MaxSpeed;
            }
        }
    }

    public override void OnStateExit()
    {
        _animator.speed = 1;
    }
}