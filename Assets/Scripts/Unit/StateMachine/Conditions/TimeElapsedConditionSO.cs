using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "TimeElapsedCondition", menuName = "State Machine/Conditions/Time Elapsed")]
public class TimeElapsedConditionSO : StateConditionSO
{
    [SerializeField] private float _timeLength = default;
    public float TimeLength => _timeLength;

    protected override Condition CreateCondition() => new TimeElapsedCondition();
}

public class TimeElapsedCondition : Condition
{
    private TimeElapsedConditionSO _originSO => OriginSO as TimeElapsedConditionSO;
    private float _startTime;

    public override void OnStateEnter()
    {
        _startTime = Time.time;
    }

    public override bool Statement() => Time.time >= _startTime + _originSO.TimeLength;
}
