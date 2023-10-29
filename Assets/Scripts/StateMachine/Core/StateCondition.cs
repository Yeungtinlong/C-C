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
}



