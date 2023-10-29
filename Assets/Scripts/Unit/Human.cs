using System.Collections;
using CNC.PathFinding;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PathDriver), typeof(Damageable), typeof(Controllable))]
public class Human : MonoBehaviour
{
    [SerializeField] private HumanConfigSO _humanConfigSO = default;
    [Tooltip("Define the man how long will stand up automatically after getting hit.")]
    [SerializeField] private float _automaticallyStandDuration = default;

    private IPathDriver _driver;
    private Damageable _damageable;
    private Controllable _controllable;
    private float _lastLayTime;
    // Represent the man is falling at this moment.
    private bool _isLaying = false;
    private bool _isLayStateProcessed = true;

    public bool IsTimeToAutomaticallyStand => Time.time >= _lastLayTime + _automaticallyStandDuration;
    public bool IsLaying => _isLaying;
    public bool ILayStateProcessed => _isLayStateProcessed;


    private void Awake()
    {
        _driver = GetComponent<IPathDriver>();
        _damageable = GetComponent<Damageable>();
        _controllable = GetComponent<Controllable>();
        SetupHumanConfig();
    }

    private void SetupHumanConfig()
    {

    }

    public void CreepStateOn()
    {
        _isLaying = true;
        FallDown();
    }

    public void CreepStateOff()
    {
        _isLaying = false;
        StandUp();
    }

    private void FallDown()
    {
        _isLaying = true;
        _driver.MaxSpeed = _humanConfigSO.LayMoveSpeed;
        _controllable.SetSightRange(_humanConfigSO.SightRangeOnLaying);
        _damageable.SetReduceDamageScale(_humanConfigSO.ReduceDamageScaleOnLaying);
    }

    public void LayByGettingHit()
    {
        _lastLayTime = Time.time;
        FallDown();
    }

    public void StandUp()
    {
        _isLaying = false;
        _driver.MaxSpeed = _humanConfigSO.MoveSpeed;
        _controllable.SetSightRange(_humanConfigSO.SightRangeOnStanding);
        _damageable.SetReduceDamageScale(_humanConfigSO.ReduceDamageScaleOnStanding);
    }
}