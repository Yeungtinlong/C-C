using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using HutongGames.PlayMaker;

[ActionCategory("Custom Scripts")]
public class StateMachineTransition : FsmStateAction
{
    [ObjectType(typeof(StateConditionSO))]
    public FsmObject conditionSO;

    public FsmEvent transitionEvent;

    public FsmBool desireValue = new FsmBool() { Value = true };

    private Condition _condition;

    public override void Awake()
    {
        _condition = ((StateConditionSO)conditionSO.Value).GetCondition();
        _condition.OnAwake(Owner.GetComponent<StateMachine>());
    }

    public override void OnEnter()
    {
        _condition.OnStateEnter();
    }

    public override void OnUpdate()
    {
        if (_condition.Statement() == desireValue.Value)
        {
            PlayMakerFSM.BroadcastEvent(transitionEvent);
        }
    }

    public override void OnExit()
    {
        _condition.OnStateExit();
    }
}