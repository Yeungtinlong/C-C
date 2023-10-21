using System;
using System.Collections.Generic;
using System.Reflection;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using HutongGames.PlayMaker;

[ActionCategory("Custom Scripts")]
public class StateMachineAction : FsmStateAction
{
    [ArrayEditor(typeof(StateActionSO))]
    public FsmArray actions;
    
    private readonly List<StateAction> _actions = new List<StateAction>();
    
    public override void Awake()
    {
        var stateActionSOs = actions.Values;
        foreach (var stateActionSO in stateActionSOs)
        {
            var createActionMethod =
                typeof(StateActionSO).GetMethod("CreateAction", BindingFlags.Instance | BindingFlags.NonPublic);
            StateAction action = (StateAction) createActionMethod.Invoke(stateActionSO, Array.Empty<object>());
            _actions.Add(action);
        }
        foreach (var action in _actions)
        {
            action.OnAwake(Owner.GetComponent<StateMachine>());
        }
    }

    public override void OnEnter()
    {
        foreach (var action in _actions)
        {
            action.OnUpdate();
        }
    }

    public override void OnUpdate()
    {
        foreach (var action in _actions)
        {
            action.OnUpdate();
        }
    }

    public override void OnExit()
    {
        foreach (var action in _actions)
        {
            action.OnStateExit();
        }
    }
}