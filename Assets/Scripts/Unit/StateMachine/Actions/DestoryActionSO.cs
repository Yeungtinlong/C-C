using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "DestoryAction", menuName = "State Machine/Actions/Destory")]
public class DestoryActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new DestoryAction();
}

public class DestoryAction : StateAction
{
    private GameObject _gameObject;
    private UnitEffectController _effectController;
    private PathDriver _driver;
    private Damageable _damageable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _gameObject = stateMachine.gameObject;
        _effectController = stateMachine.GetComponent<UnitEffectController>();
        _driver = stateMachine.GetComponent<PathDriver>();
        _damageable = stateMachine.GetComponent<Damageable>();
    }

    public override void OnStateEnter()
    {
        if (_damageable.UnitType == UnitType.Infantry)
        {
            GameObject.Destroy(_gameObject);
            return;
        }
        
        _effectController.PlayDieEffect();
        _driver.Stop(false);
        GameObject.Destroy(_gameObject, 15f);
    }
}