using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class Damageable : MonoBehaviour
{
    [SerializeField] private FactionType _faction = default;
    [SerializeField] private UnitType _unitType = default;
    [SerializeField] private DamageableType _damageableType = default;
    //[SerializeField] private UnitType _unitType = default;
    [SerializeField] private HealthConfigSO _healthConfigSO = default;
    [SerializeField] private DamageableUICanvas _damageableUICanvas = default;
    [Header("Broadcasting on")]
    [SerializeField] private CountUnitEventChannelSO _countUnitChannel = default;
    private int _unitID;
    private int _currentHealth = default;
    private Controllable _controllable;
    private float _reduceDamageScale = 1f;
    private bool _isLowHealth;
    private BoxCollider _collider;

    public FactionType Faction => _faction;
    public UnitType UnitType => _unitType;
    public DamageableType DamageableType => _damageableType;
    public int CurrentHealth => _currentHealth;
    public int UnitID => _unitID;
    public Controllable Controllable => _controllable;

    public bool IsGettingHit { get; set; } = false;
    public bool IsLowHealth => _isLowHealth;
    public bool IsDead { get; set; } = false;

    public event UnityAction<Damageable> OnDie;

    private void Awake()
    {
        _currentHealth = _healthConfigSO.MaxHealth;
        _controllable = GetComponent<Controllable>();
        _collider = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        OnDie += CloseColliderOnDie;
    }

    private void OnDisable()
    {
        OnDie -= CloseColliderOnDie;
    }

    private void Start()
    {
        _unitID = _countUnitChannel.RaiseEvent(this);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
            return;

        _currentHealth -= Mathf.FloorToInt(damage * _reduceDamageScale);
        IsGettingHit = true;

        _damageableUICanvas.SetHealthValue(_currentHealth, _healthConfigSO.MaxHealth);

        if (_currentHealth <= 0)
        {
            IsDead = true;
            if (OnDie != null)
                OnDie.Invoke(this);
        }
        else if(_currentHealth < _healthConfigSO.MaxHealth * 0.5f)
        {
            _isLowHealth = true;
        }
    }

    public void ChangeFaction(FactionType faction)
    {
        _faction = faction;
    }

    public void ToggleHealthBar(bool isToggled, bool isAllies)
    {
        _damageableUICanvas.ToggleHealthBar(isToggled, isAllies);
        if (isToggled)
            _damageableUICanvas.SetHealthValue(_currentHealth, _healthConfigSO.MaxHealth);
    }

    public void SetReduceDamageScale(float reduceDamageScale)
    {
        _reduceDamageScale = reduceDamageScale;
    }

    private void CloseColliderOnDie(Damageable damageable)
    {
        _collider.enabled = false;
    }
}

public enum FactionType { None, GDI, Nod }
public enum DamageableType { Human, Vehicle, Construction }
public enum UnitAlignment
{
    None = -1,
    Neutral,
    Own,
    Allied,
    Enemy
}

[Flags]
public enum UnitType
{
    None = 0,
    Infantry = 1,
    Light = 2,
    Vehicle = 4,
    Aircraft = 8,
    Building = 16,
    Armor = 32,
    Obstacle = 64
}