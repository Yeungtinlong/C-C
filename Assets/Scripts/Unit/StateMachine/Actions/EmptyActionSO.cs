using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(menuName = "State Machine/Actions/Empty")]
public class EmptyActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new EmptyAction();
}

public class EmptyAction : StateAction { }