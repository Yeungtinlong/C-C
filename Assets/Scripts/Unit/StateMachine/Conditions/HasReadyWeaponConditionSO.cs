using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "HasReadyWeaponCondition", menuName = "State Machine/Conditions/Has Ready Weapon")]
public class HasReadyWeaponConditionSO : StateConditionSO
{
    [SerializeField] private float _delay = default;
    public float Delay => _delay;

    protected override Condition CreateCondition() => new HasReadyWeaponCondition();
}

public class HasReadyWeaponCondition : Condition
{
    private HasReadyWeaponConditionSO _originSO => OriginSO as HasReadyWeaponConditionSO;

    private Attacker _attacker = default;    
    private float _delay = default;
    private float _timer = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
        _delay = _originSO.Delay;
    }

    public override void OnStateEnter()
    {
        _timer = 0f;
    }

    public override bool Statement()
    {
        _timer += Time.deltaTime;
        if (_timer < _delay)
            return false;

        _timer = 0f;
        _attacker.UpdateReadyWeaponInfo();

        bool[] isWeaponReadyGroup = _attacker.IsWeaponReadyGroup;
        int count = isWeaponReadyGroup.Length;
        bool result = false;

        for (int i = 0; i < count && !result; i++)
        {
            result = isWeaponReadyGroup[i] || result;
        }

        return result;
    }
}