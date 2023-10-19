using System;
using System.Collections.Generic;
using System.Reflection;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using HutongGames.PlayMaker;

[Serializable]
public class StateTransitionConfig
{
    public StateConditionSO StateConditionSO;
    public FsmEvent Event;

    public StateTransition CreateTransition() => new StateTransition()
    {
        Condition = StateConditionSO.GetCondition(),
        Event = Event
    };
}

public class StateTransition
{
    public Condition Condition;
    public FsmEvent Event;
}

[ActionCategory("Custom Scripts")]
public class StateMachineAction : FsmStateAction
{
    [ArrayEditor(typeof(StateTransitionConfig))]
    public FsmArray transitions;
    
    [ArrayEditor(typeof(StateActionSO))]
    public FsmArray actions;

    private readonly List<StateTransition> _transitions = new List<StateTransition>();
    private readonly List<StateAction> _actions = new List<StateAction>();
    
    public override void Awake()
    {
        var stateTransitionConfigs = transitions.Values;
        foreach (var stateTransitionConfig in stateTransitionConfigs)
        {
            _transitions.Add(((StateTransitionConfig)stateTransitionConfig).CreateTransition());
        }

        var stateActionSOs = actions.Values;
        foreach (var stateActionSO in stateActionSOs)
        {
            var createActionMethod =
                typeof(StateActionSO).GetMethod("CreateAction", BindingFlags.Instance | BindingFlags.NonPublic);
            StateAction action = (StateAction) createActionMethod.Invoke(stateActionSO, Array.Empty<object>());
            _actions.Add(action);
        }
        
        foreach (var transition in _transitions)
        {
            transition.Condition.OnAwake(Owner.GetComponent<StateMachine>());
        }

        foreach (var action in _actions)
        {
            action.OnAwake(Owner.GetComponent<StateMachine>());
        }
    }

    public override void OnEnter()
    {
        foreach (var transition in _transitions)
        {
            transition.Condition.OnStateEnter();
        }

        foreach (var action in _actions)
        {
            action.OnUpdate();
        }
    }

    public override void OnUpdate()
    {
        foreach (var transition in _transitions)
        {
            if (transition.Condition.Statement())
            {
                PlayMakerFSM.BroadcastEvent(transition.Event);
                return;
            }
        }

        foreach (var action in _actions)
        {
            action.OnUpdate();
        }
    }

    public override void OnExit()
    {
        foreach (var transition in _transitions)
        {
            transition.Condition.OnStateExit();
        }

        foreach (var action in _actions)
        {
            action.OnStateExit();
        }
    }
}