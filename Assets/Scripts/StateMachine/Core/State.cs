using CNC.StateMachine.ScriptableObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.StateMachine
{
    public class State
    {
        private StateSO _originSO;
        public StateSO Origin { get => _originSO; set => _originSO = value; }

        private StateMachine _stateMachine;
        private StateTransition[] _transitions;
        private StateAction[] _stateActions;

        internal StateMachine StateMachine { get => _stateMachine; set => _stateMachine = value; }
        internal StateTransition[] Transitions { get => _transitions; set => _transitions = value; }
        internal StateAction[] StateActions { get => _stateActions; set => _stateActions = value; }

        internal State() { }

        public State(StateSO originSO, StateMachine stateMachine, StateTransition[] transitions, StateAction[] stateActions)
        {
            _originSO = originSO;
            _stateMachine = stateMachine;
            _transitions = transitions;
            _stateActions = stateActions;
        }

        public void OnStateEnter()
        {
            void OnStateEnter(IStateComponent[] comps)
            {
                for (int i = 0; i < comps.Length; i++)
                {
                    comps[i].OnStateEnter();
                }
            }

            OnStateEnter(_transitions);
            OnStateEnter(_stateActions);
        }

        public void OnStateExit()
        {
            void OnStateExit(IStateComponent[] comps)
            {
                for (int i = 0; i < comps.Length; i++)
                {
                    comps[i].OnStateExit();
                }
            }

            OnStateExit(_transitions);
            OnStateExit(_stateActions);
        }

        public void OnUpdate()
        {
            for (int i = 0; i < _stateActions.Length; i++)
            {
                _stateActions[i].OnUpdate();
            }
        }

        public bool TryGetTransition(out State state)
        {
            state = null;

            for (int i = 0; i < _transitions.Length; i++)
                if (_transitions[i].TryGetTransition(out state))
                    break;

            for (int i = 0; i < _transitions.Length; i++)
                _transitions[i].ClearConditionsCache();

            return state != null;
        }
    }
}

