using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "LogAction", menuName = "State Machine/Actions/LogAction")]
public class LogActionSO : StateActionSO
{
    [SerializeField] private string _message;
    public string Message => _message;

    protected override StateAction CreateAction() => new LogAction();
}

public class LogAction : StateAction
{
    LogActionSO _originSO => OriginSO as LogActionSO;

    public override void OnUpdate()
    {
        Debug.Log(_originSO.Message);
    }
}