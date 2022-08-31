using CNC.StateMachine.ScriptableObjects;

namespace CNC.StateMachine
{
    public abstract class StateAction : IStateComponent
    {
        internal StateActionSO _originSO;
        protected StateActionSO OriginSO => _originSO;

        public virtual void OnAwake(StateMachine stateMachine) { }

        public virtual void OnStateEnter() { }

        public virtual void OnStateExit() { }

        public virtual void OnUpdate() { }

        public enum SpecificMoment { OnStateEnter, OnStateExit, OnUpdate }
    }
}

