using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.StateMachine.ScriptableObjects
{
    public abstract class StateActionSO : ScriptableObject
    {
        public StateAction GetAction(StateMachine stateMachine, Dictionary<ScriptableObject, object> createdInstances)
        {
            if (createdInstances.TryGetValue(this, out object obj))
                return obj as StateAction;

            StateAction action = CreateAction();
            createdInstances.Add(this, action);
            action._originSO = this;
            action.OnAwake(stateMachine);

            return action;
        }

        protected abstract StateAction CreateAction();

        public StateAction GetAction()
        {
            var action = CreateAction();
            action._originSO = this;
            return action;
        }
    }
}

