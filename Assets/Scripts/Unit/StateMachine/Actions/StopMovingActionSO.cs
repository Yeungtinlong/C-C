using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "StopMovingAction", menuName = "State Machine/Actions/Stop Moving")]
public class StopMovingActionSO : StateActionSO
{
    public bool IsInstantStop;
    protected override StateAction CreateAction() => new StopMovingAction();
}

public class StopMovingAction : StateAction
{
    private StopMovingActionSO _originSO => OriginSO as StopMovingActionSO;
    private PathDriver _driver;

    public override void OnAwake(StateMachine stateMachine)
    {
        _driver = stateMachine.GetComponent<PathDriver>();
    }

    public override void OnStateEnter()
    {
        _driver.Stop(_originSO.IsInstantStop);
    }
}