using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;
using SpecificMoment = CNC.StateMachine.StateAction.SpecificMoment;

[CreateAssetMenu(fileName = "AnimatorParameterAction", menuName = "State Machine/Actions/Animator Parameter")]
public class AnimatorParameterActionSO : StateActionSO
{
    [SerializeField] private ParameterType _parameterType = default;
    [SerializeField] private string _parameterName = default;

    [SerializeField] private bool _boolValue = default;
    [SerializeField] private int _intValue = default;
    [SerializeField] private float _floatValue = default;
    [SerializeField] private bool _isUseOtherAnimator = default;

    [SerializeField] private SpecificMoment _specificMoment = default;

    public ParameterType ParameterType => _parameterType;
    public string ParameterName => _parameterName;

    public bool BoolValue => _boolValue;
    public int IntValue => _intValue;
    public float FloatValue => _floatValue;

    public bool IsUseOtherAnimator => _isUseOtherAnimator;

    public SpecificMoment SpecificMoment => _specificMoment;

    protected override StateAction CreateAction() => new AnimatorParameterAction(Animator.StringToHash(_parameterName));
}

public enum ParameterType { Bool, Int, Float, Trigger }

public class AnimatorParameterAction : StateAction
{
    private AnimatorParameterActionSO _originSO => OriginSO as AnimatorParameterActionSO;
    private Animator _animator = default;
    private int _parameterHash = default;

    public AnimatorParameterAction(int parameterHash)
    {
        _parameterHash = parameterHash;
    }

    public override void OnAwake(StateMachine stateMachine)
    {
        if (_originSO.IsUseOtherAnimator)
            _animator = stateMachine.GetComponent<AnimatorController>().ManualAnimator;
        else
            _animator = stateMachine.GetComponent<Animator>();
    }

    public override void OnStateEnter()
    {
        if (_originSO.SpecificMoment == SpecificMoment.OnStateEnter)
            SetParameter();
    }

    public override void OnStateExit()
    {
        if (_originSO.SpecificMoment == SpecificMoment.OnStateExit)
            SetParameter();
    }

    private void SetParameter()
    {
        switch (_originSO.ParameterType)
        {
            case ParameterType.Bool:
                _animator.SetBool(_parameterHash, _originSO.BoolValue);
                break;
            case ParameterType.Int:
                _animator.SetInteger(_parameterHash, _originSO.IntValue);
                break;
            case ParameterType.Float:
                _animator.SetFloat(_parameterHash, _originSO.FloatValue);
                break;
            case ParameterType.Trigger:
                _animator.SetTrigger(_parameterHash);
                break;
        }
    }
}