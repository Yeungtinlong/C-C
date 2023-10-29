using CNC.PathFinding;
using CNC.StateMachine;
using CNC.StateMachine.ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "DestroyAction", menuName = "State Machine/Actions/Destroy")]
public class DestroyActionSO : StateActionSO
{
    protected override StateAction CreateAction() => new DestroyAction();
}

public class DestroyAction : StateAction
{
    private GameObject _gameObject;
    private UnitEffectController _effectController;
    private IPathDriver _driver;
    private Damageable _damageable;

    public override void OnAwake(StateMachine stateMachine)
    {
        _gameObject = stateMachine.gameObject;
        _effectController = stateMachine.GetComponent<UnitEffectController>();
        _driver = stateMachine.GetComponent<IPathDriver>();
        _damageable = stateMachine.GetComponent<Damageable>();
    }

    public override void OnStateEnter()
    {
        if (_damageable.UnitType == UnitType.Infantry)
        {
            Object.Destroy(_gameObject);
            return;
        }
        
        _effectController.PlayDieEffect();
        _driver.Stop(false);
        Object.Destroy(_gameObject, 15f);
        _damageable.IsDestroyed = true;
    }
}