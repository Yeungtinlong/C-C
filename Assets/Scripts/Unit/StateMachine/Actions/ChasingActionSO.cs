using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using CNC.PathFinding;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "ChasingAction", menuName = "State Machine/Actions/Chasing")]
public class ChasingActionSO : StateActionSO
{
    public float ChaseThreshold;
    protected override StateAction CreateAction() => new ChasingAction();
}

public class ChasingAction : StateAction
{
    private ChasingActionSO _origin => OriginSO as ChasingActionSO;
    private Controllable _controllable;
    private Attacker _attacker;
    private IPathDriver _driver;
    private Animator _animator;

    public override void OnAwake(StateMachine stateMachine)
    {
        _controllable = stateMachine.GetComponent<Controllable>();
        _attacker = stateMachine.GetComponent<Attacker>();
        _driver = stateMachine.GetComponent<IPathDriver>();
        _animator = stateMachine.TryGetComponent(out _animator)
            ? _animator
            : stateMachine.GetComponent<AnimatorController>().ManualAnimator;
    }

    public override void OnUpdate()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Chase)
            {
                if (command.Target == null)
                    return;

                _driver.SetDestination(command.Destination, command.TargetAlignment, false,
                    _attacker.ChaseDistance - _origin.ChaseThreshold);

                _animator.speed = _driver.CurrentSpeed / _driver.MaxSpeed;
            }
        }
    }

    public override void OnStateExit()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Chase)
            {
                if (command.Target == null || command.Target.IsDead)
                {
                    _controllable.RemoveCurrentCommand();
                }
            }
        }

        _animator.speed = 1f;
    }
}