using System;
using UnityEngine;

namespace CNC.StateMachine.Debugging
{
    [Serializable]
    public class StateMachineDebugger
    {
        [SerializeField] private string _currentState;
        public string CurrentState { get => _currentState; set => _currentState = value; }

        public void OnAwake(StateMachine stateMachine)
        {
            _currentState = stateMachine.CurrentState.Origin.name;
        }

        public void OnTransitionSuccess(string targetState)
        {
            _currentState = targetState;
        }
    }
}
