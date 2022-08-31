using UnityEngine;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "AttackAction", menuName = "State Machine/Actions/Attack")]
public class AttackActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new AttackAction();
}

public class AttackAction : StateAction
{
    private Attacker _attacker = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
    }

    public override void OnStateEnter()
    {
        _attacker.Attack();
    }
}


