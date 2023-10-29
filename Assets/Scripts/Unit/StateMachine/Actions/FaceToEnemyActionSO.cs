using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "FaceToEnemy", menuName = "State Machine/Actions/Face To Enemy")]
public class FaceToEnemyActionSO : StateActionSO
{
    [SerializeField] private float _angleThreshold = default;
    public float AngleThreshold => _angleThreshold;
    protected override StateAction CreateAction() => new FaceToEnemyAction();
}

public class FaceToEnemyAction : StateAction
{
    private Transform _transform = default;
    private Attacker _attacker = default;
    private Controllable _controllable = default;
    private IPathDriver _pathDriver = default;

    FaceToEnemyActionSO _originSO => (FaceToEnemyActionSO)OriginSO;

    public override void OnAwake(StateMachine stateMachine)
    {
        _transform = stateMachine.transform;
        _attacker = stateMachine.GetComponent<Attacker>();
        _controllable = stateMachine.GetComponent<Controllable>();
        _pathDriver = stateMachine.GetComponent<IPathDriver>();
    }

    public override void OnUpdate()
    {
        if (_attacker.CurrentEnemy == null || _attacker.CurrentEnemy.IsDead)
            return;

        // Vector3 direction = (_attacker.CurrentEnemy.transform.position - _transform.position).normalized;
        // if (_pathDriver.IsNewAlignment(direction))
        // {
        //     Command command = new Command { CommandType = CommandType.Rotate, Destination = _transform.position, TargetAlignment = direction, IsForcedAlign = true };
        //     _controllable.AddCommand(command, true);
        // }
        
        _pathDriver.SetInPlaceTurn((_attacker.CurrentEnemy.transform.position - _transform.position).normalized);
    }
}