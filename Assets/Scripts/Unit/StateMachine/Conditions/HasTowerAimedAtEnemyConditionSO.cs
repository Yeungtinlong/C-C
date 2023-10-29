using UnityEngine;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "HasTowerAimedAtEnemy", menuName = "State Machine/Conditions/Has Tower Aimed At Enemy")]
public class HasTowerAimedAtEnemyConditionSO : StateConditionSO
{
    [SerializeField] private float _threshold = default;
    public float Threshold => _threshold;
    protected override Condition CreateCondition() => new HasTowerAimedAtEnemyCondition();
}

public class HasTowerAimedAtEnemyCondition : Condition
{
    private Attacker _attacker;
    private RotatableTower _rotatableTower;
    private Transform _towerAnchor;

    private HasTowerAimedAtEnemyConditionSO _originSO => OriginSO as HasTowerAimedAtEnemyConditionSO;

    public override void OnAwake(StateMachine stateMachine)
    {
        _attacker = stateMachine.GetComponent<Attacker>();
        _rotatableTower = stateMachine.GetComponent<RotatableTower>();
        _towerAnchor = _rotatableTower.TowerAnchor;
    }

    public override bool Statement()
    {
        if (_attacker.CurrentEnemy != null)
        {
            Vector3 towerOrientation = _towerAnchor.forward;
            towerOrientation.y = 0f;
            Vector3 towerPos = _towerAnchor.position;
            towerPos.y = 0f;
            Vector3 enemyPos = _attacker.CurrentEnemy.transform.position;
            enemyPos.y = 0f;
            Vector3 enemyDir = (enemyPos - towerPos).normalized;

            return Vector3.Angle(towerOrientation, enemyDir) <= _originSO.Threshold;
        }

        return false;
    }
}