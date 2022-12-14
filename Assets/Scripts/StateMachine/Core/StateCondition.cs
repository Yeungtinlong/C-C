using CNC.StateMachine.ScriptableObjects;

namespace CNC.StateMachine
{
    public readonly struct StateCondition
    {
        private readonly StateMachine _stateMachine;
        private readonly Condition _condition;
        private readonly bool _expectedResult;

        public StateMachine StateMachine => _stateMachine;
        public Condition Condition => _condition;
        public bool ExpectedResult => _expectedResult;

        public StateCondition(StateMachine stateMachine, Condition condition, bool expectedResult)
        {
            _stateMachine = stateMachine;
            _condition = condition;
            _expectedResult = expectedResult;
        }

        public bool IsMet()
        {
            bool statement = _condition.GetStatement();
            bool isMet = _expectedResult == statement;

            return isMet;
        }
    }

    public abstract class Condition : IStateComponent
    {
        internal StateConditionSO _originSO;
        protected StateConditionSO OriginSO => _originSO;

        public bool IsCached { get; set; }
        public bool _cachedStatement = default;

        public virtual void OnAwake(StateMachine stateMachine) { }

        public virtual void OnStateEnter() { }

        public virtual void OnStateExit() { }

        public abstract bool Statement();

        public bool GetStatement()
        {
            if (!IsCached)
            {
                _cachedStatement = Statement();
                IsCached = true;
            }

            return _cachedStatement;
        }

        public void ClearCachedStatement()
        {
            _cachedStatement = false;
            IsCached = false;
        }
    }
}



