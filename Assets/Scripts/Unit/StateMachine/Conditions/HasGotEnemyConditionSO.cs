using UnityEngine;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "HasGotEnemy", menuName = "State Machine/Conditions/Has Got Enemy")]
public class HasGotEnemyConditionSO : StateConditionSO
{
    [SerializeField] private float _delay = default;
    public float Delay => _delay;

    protected override Condition CreateCondition() => new HasGotEnemyCondition();
}

public class HasGotEnemyCondition : Condition
{
    private HasGotEnemyConditionSO _originSO => OriginSO as HasGotEnemyConditionSO;

    private Attacker _attacker = default;
    private Damageable _damageable = default;
    private float _delay = default;
    private float _timer = default;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
        _damageable = stateMachine.GetComponent<Damageable>();
        _delay = _originSO.Delay;
    }

    public override void OnStateEnter()
    {
        _timer = 0f;
    }

    public override bool Statement()
    {
        Damageable currentEnemy = _attacker.CurrentEnemy;
        if (currentEnemy != null && !currentEnemy.IsDead)
        {
            _timer = 0f;
            return true;
        }
    
        _timer += Time.deltaTime;
        if (_timer < _delay)
            return false;

        _timer = 0f;
        if (_attacker.TryGetEnemy(_damageable.Faction, out Damageable enemy))
        {
            _attacker.SetCurrentEnemy(enemy);
            return true;
        }

        return false;
    }
}
