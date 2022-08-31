using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerRotateToForwardAction", menuName = "State Machine/Actions/Tower Rotate To Forward")]
public class TowerRotateToForwardActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new TowerRotateToForwardAction();
}

public class TowerRotateToForwardAction : StateAction
{
    private Transform _transform;
    private RotatableTower _rotatableTower;
    private Attacker _attacker;

    public override void OnAwake(StateMachine stateMachine)
    {
        _transform = stateMachine.transform;
        _rotatableTower = stateMachine.GetComponent<RotatableTower>();
        _attacker = stateMachine.GetComponent<Attacker>();
    }

    public override void OnUpdate()
    {
        if (_attacker.CurrentEnemy == null || _attacker.CurrentEnemy.IsDead)
        {
            _rotatableTower.RotateTo(_transform.forward);
        }
    }
}