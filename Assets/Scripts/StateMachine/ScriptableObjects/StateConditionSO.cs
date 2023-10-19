using System.Collections.Generic;
using UnityEngine;

namespace CNC.StateMachine.ScriptableObjects
{
    public abstract class StateConditionSO : ScriptableObject
    {
        public StateCondition GetCondition(StateMachine stateMachine, bool expectedResult, Dictionary<ScriptableObject, object> createdInstances)
        {
            if (createdInstances.TryGetValue(this, out object obj))
                return new StateCondition(stateMachine, obj as Condition, expectedResult);

            Condition condition = CreateCondition();
            condition._originSO = this;
            condition.OnAwake(stateMachine);
            createdInstances.Add(this, condition);

            return new StateCondition(stateMachine, condition, expectedResult);
        }

        protected abstract Condition CreateCondition();

        public Condition GetCondition() => CreateCondition();
    }
}
