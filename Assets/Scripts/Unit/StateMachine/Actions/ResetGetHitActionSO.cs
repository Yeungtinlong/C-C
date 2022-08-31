using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "ResetGetHitAction", menuName = "State Machine/Actions/Reset Get Hit")]
public class ResetGetHitActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new ResetGetHitAction();
}

public class ResetGetHitAction : StateAction
{
    private Damageable _damageable = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _damageable = stateMachine.GetComponent<Damageable>();
    }

    public override void OnStateEnter()
    {
        _damageable.IsGettingHit = false;
    }
}