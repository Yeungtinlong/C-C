using UnityEngine;
using CNC.StateMachine.ScriptableObjects;
using CNC.StateMachine.Debugging;

namespace CNC.StateMachine
{
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private TransitionTableSO _transitionTable = default;

        private State _currentState = default;
        public State CurrentState => _currentState;
        //private State[] _states = default;

#if UNITY_EDITOR
        [Space]
        [SerializeField] private StateMachineDebugger _debugger = default;
        public StateMachineDebugger Debugger { get => _debugger; set => _debugger = value; }
#endif

        private void Awake()
        {
            _currentState = GetInitialState(this);
#if UNITY_EDITOR
            _debugger.OnAwake(this);
#endif
        }

        private void Start()
        {
            _currentState.OnStateEnter();
        }

        private void Update()
        {
            if (_currentState.TryGetTransition(out State transitionState))
                Transition(transitionState);

            _currentState.OnUpdate();
        }

        private State GetInitialState(StateMachine stateMachine)
        {
            return _transitionTable.GetInitialState(stateMachine);
        }

        private void Transition(State transitionState)
        {
            _currentState.OnStateExit();
            _currentState = transitionState;
            _currentState.OnStateEnter();
        }
    }
}