using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.StateMachine.ScriptableObjects
{
    [CreateAssetMenu(menuName = "State Machine/State")]
    public class StateSO : ScriptableObject
    {
        [SerializeField] private StateActionSO[] _actions = default;

        public State GetState(StateMachine stateMachine, Dictionary<ScriptableObject, object> createdInstances)
        {
            if (createdInstances.TryGetValue(this, out object obj))
                return obj as State;

            State state = new State();
            createdInstances.Add(this, state);

            state.Origin = this;
            state.StateMachine = stateMachine;
            state.Transitions = new StateTransition[0];
            state.StateActions = GetActions(_actions, stateMachine, createdInstances);

            return state;
        }

        private StateAction[] GetActions(StateActionSO[] actions, StateMachine stateMachine, Dictionary<ScriptableObject, object> createdInstances)
        {
            StateAction[] stateActions = new StateAction[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                stateActions[i] = actions[i].GetAction(stateMachine, createdInstances);
            }

            return stateActions;
        }
    }
}

