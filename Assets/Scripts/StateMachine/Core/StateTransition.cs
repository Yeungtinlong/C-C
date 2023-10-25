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

            // ���ÿһ��Ľ��
            for (int i = 0, idx = 0; i < count && idx < _stateConditions.Length; i++)
            {
                // i�����ڼ��飬j��������ĵڼ���condition��idx������ǰcondition��ȫ��condition�е�����
                for (int j = 0; j < _resultGroups[i]; j++, idx++)
                {
                    // ���� "and" ����
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

            // ��֮�� "or" ���㣬!result����ֻҪ������true�Ϳ�����ǰ����ѭ��
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

