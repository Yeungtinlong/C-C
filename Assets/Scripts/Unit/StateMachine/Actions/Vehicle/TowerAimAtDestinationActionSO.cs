using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerAimAtDestinationAction", menuName = "State Machine/Actions/Tower Aim At Destination")]
public class TowerAimAtDestinationActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new TowerAimAtDestinationAction();
}

public class TowerAimAtDestinationAction : StateAction
{
    private Transform _transform;
    private RotatableTower _rotatableTower;
    private Controllable _controllable;
    private Attacker _attacker;

    public override void OnAwake(StateMachine stateMachine)
    {
        _transform = stateMachine.transform;
        _rotatableTower = stateMachine.GetComponent<RotatableTower>();
        _controllable = stateMachine.GetComponent<Controllable>();
        _attacker = stateMachine.GetComponent<Attacker>();
    }

    public override void OnUpdate()
    {
        if (_controllable.TryGetCurrentCommand(out Command command))
        {
            if (command.CommandType == CommandType.Move || command.CommandType == CommandType.Chase)
            {
                if (_attacker.CurrentEnemy == null || _attacker.CurrentEnemy.IsDead)
                {
                    Vector3 dir = (command.Destination - _transform.position).normalized;
                    if (dir == Vector3.zero)
                    {
                        dir = _transform.forward;
                    }
                    _rotatableTower.RotateTo(dir);
                }
            }
        }
    }
}
