using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.StateMachine
{
    public class StateTransition : IStateComponent
    {
        private State _targetState;
        private StateCondition[] _stateConditions;
        private int[] _resultGroups = default;
        private bool[] _results = default;

        public State TargetState { get => _targetState; set => _targetState = value; }
        public StateCondition[] StateConditions { get => _stateConditions; set => _stateConditions = value; }

        internal StateTransition() { }
        public StateTransition(State targetState, StateCondition[] stateConditions, int[] resultGroups)
        {
            _targetState = targetState;
            _stateConditions = stateConditions;
            _resultGroups = resultGroups != null && resultGroups.Length > 0 ? resultGroups : new int[1];
            _results = new bool[resultGroups.Length];
        }

        public void OnStateEnter()
        {
            for (int i = 0; i < _stateConditions.Length; i++)
                _stateConditions[i].Condition.OnStateEnter();
        }

        public void OnStateExit()
        {
            for (int i = 0; i < _stateConditions.Length; i++)
                _stateConditions[i].Condition.OnStateExit();
        }

        public bool TryGetTransition(out State state)
        {
            state = ShouldTransition() ? _targetState : null;
            return state != null;
        }

        private bool ShouldTransition()
        {
            int count = _resultGroups.Length;

            // 算出每一组的结果
            for (int i = 0, idx = 0; i < count && idx < _stateConditions.Length; i++)
            {
                // i代表第几组，j代表该组的第几个condition，idx代表当前condition在全部condition中的索引
                for (int j = 0; j < _resultGroups[i]; j++, idx++)
                {
                    // 组内 "and" 运算
                    if (j == 0)
                    {
                        _results[i] = _stateConditions[idx].IsMet();
                    }
                    else
                    {
                        _results[i] = _stateConditions[idx].IsMet() && _results[i];
                    }
                }
            }

            bool result = false;

            // 组之间 "or" 运算，!result代表只要出现了true就可以提前结束循环
            for (int i = 0; i < _results.Length && !result; i++)
            {
                result = _results[i] || result;
            }

#if UNITY_EDITOR
            if (result)
                _targetState.StateMachine.Debugger.OnTransitionSuccess(_targetState.Origin.name);
#endif
            return result;
        }

        public void ClearConditionsCache()
        {
            for (int i = 0; i < _stateConditions.Length; i++)
            {
                _stateConditions[i].Condition.ClearCachedStatement();
            }
        }
    }
}

