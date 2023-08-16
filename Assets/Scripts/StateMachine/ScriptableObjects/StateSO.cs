using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CNC.StateMachine.ScriptableObjects
{
    [CreateAssetMenu(menuName = "State Machine/State")]
    public class StateSO : ScriptableObject
    {
// #if UNITY_EDITOR
        [HideInInspector] [SerializeField] public string stateName;
        [HideInInspector] [SerializeField] public Vector2 position;

        [HideInInspector] [SerializeField] public string guid;
// #endif

        [SerializeField] private List<StateActionSO> _actions = default;
        public List<StateActionSO> Actions => _actions;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = GUID.Generate().ToString();
            }
        }

        public State GetState(StateMachine stateMachine, Dictionary<ScriptableObject, object> createdInstances)
        {
            if (createdInstances.TryGetValue(this, out object obj))
                return obj as State;

            State state = new State();
            createdInstances.Add(this, state);

            state.Origin = this;
            state.StateMachine = stateMachine;
            state.Transitions = Array.Empty<StateTransition>();
            state.StateActions = GetActions(_actions, stateMachine, createdInstances);

            return state;
        }

        private StateAction[] GetActions(List<StateActionSO> actions, StateMachine stateMachine,
            Dictionary<ScriptableObject, object> createdInstances)
        {
            StateAction[] stateActions = new StateAction[actions.Count];
            for (int i = 0; i < actions.Count; i++)
            {
                stateActions[i] = actions[i].GetAction(stateMachine, createdInstances);
            }

            return stateActions;
        }
    }
}