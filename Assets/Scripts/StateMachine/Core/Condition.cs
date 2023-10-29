using CNC.StateMachine.ScriptableObjects;

namespace CNC.StateMachine
{
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
            return Statement();
            
            // TODO: Remove Cache Judgement to use VS StateMachine.
            // if (!IsCached)
            // {
            //     _cachedStatement = Statement();
            //     IsCached = true;
            // }
            //
            // return _cachedStatement;
        }

        public void ClearCachedStatement()
        {
            _cachedStatement = false;
            IsCached = false;
        }
    }
}