using System;
using UnityEngine;

namespace CNC.StateMachine.ScriptableObjects
{
    public class TransitionItem : ScriptableObject
    {
        [HideInInspector] public string guid;
        [HideInInspector] public StateSO FromState;
        [HideInInspector] public StateSO ToState;
        public ConditionUsage[] Conditions;
    }

    [Serializable]
    public struct ConditionUsage
    {
        public Result ExpectedResult;
        public StateConditionSO StateCondition;
        public Operator Operator;
    }

    public enum Result
    {
        True,
        False
    }

    public enum Operator
    {
        And,
        Or
    }
}