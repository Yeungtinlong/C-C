using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerAimAtEnemyAction", menuName = "State Machine/Actions/Tower Aim At Enemy")]
public class TowerAimAtEnemyActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new TowerAimAtEnemyAction();
}

public class TowerAimAtEnemyAction : StateAction
{
    private Transform _transform = default;
    private RotatableTower _rotatableTower = default;
    private Attacker _attacker = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _transform = stateMachine.transform;
        _rotatableTower = stateMachine.GetComponent<RotatableTower>();
        _attacker = stateMachine.GetComponent<Attacker>();
    }

    public override void OnUpdate()
    {
        if (_attacker.CurrentEnemy == null)
            return;

        _rotatableTower.RotateTo(Vector3.Normalize(_attacker.CurrentEnemy.transform.position - _transform.position));
    }
}