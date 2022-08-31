using UnityEngine;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "HasAimedAtEnemy", menuName = "State Machine/Conditions/Has Aimed At Enemy")]
public class HasAimedAtEnemyConditionSO : StateConditionSO
{
    [SerializeField] private float _threshold = default;
    public float Threshold => _threshold;
    protected override Condition CreateCondition() => new HasAimedAtEnemyCondition();
}

public class HasAimedAtEnemyCondition : Condition
{
    private Attacker _attacker = default;
    private Transform _transform = default;

    private HasAimedAtEnemyConditionSO _originSO => OriginSO as HasAimedAtEnemyConditionSO;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
        _transform = stateMachine.transform;
    }

    public override bool Statement()
    {
        return _attacker.CurrentEnemy != null
            && Vector3.Angle(_transform.forward, Vector3.Normalize(_attacker.CurrentEnemy.transform.position - _transform.position)) <= _originSO.Threshold;
    }
}