using System;
using System.Collections;
using UnityEngine;
using CNC.StateMachine;

public class Attacker : MonoBehaviour
{
    [SerializeField] private WeaponItem[] _weaponItems = default;
    [SerializeField] private Damageable _currentEnemy = default;
    private WeaponUsage[] _weaponUsages = default;
    private bool _isAntiAircraft = default;
    private float _maxAttackDistance = default;
    private float _chaseDistance;
    private bool[] _isWeaponReadyGroup = default;
    private int _layer;
    private Damageable _ownerDamageable;

    public Damageable CurrentEnemy => _currentEnemy;
    public WeaponUsage[] WeaponUsages => _weaponUsages;
    public bool IsAntiAircraft => _isAntiAircraft;
    public float MaxAttackDistance => _maxAttackDistance;
    public bool[] IsWeaponReadyGroup => _isWeaponReadyGroup;

    public float ChaseDistance => _chaseDistance;
    public Damageable OwnerDamageable => _ownerDamageable;

    private void Awake()
    {
        _weaponUsages = GetInitialWeapons(_weaponItems);
        _layer = LayerMask.NameToLayer("Unit");
        _ownerDamageable = GetComponent<Damageable>();
    }

    public void Attack()
    {
        StartCoroutine(DelayAttack(0.2f));
    }

    private IEnumerator DelayAttack(float delay)
    {
        for (int i = 0; i < IsWeaponReadyGroup.Length; i++)
        {
            yield return new WaitForSeconds(delay);
            if (CurrentEnemy == null)
            {
                _isWeaponReadyGroup = new bool[WeaponUsages.Length];
                yield break;
            }

            if (IsWeaponReadyGroup[i])
            {
                Weapon weapon = _weaponUsages[i].Weapon;
                weapon.Attack();
                weapon.IsValid = false;
                IsWeaponReadyGroup[i] = false;
                StartCoroutine(weapon.Reload());
            }
        }
    }

    public void UpdateReadyWeaponInfo()
    {
        int count = WeaponUsages.Length;
        if (CurrentEnemy == null)
        {
            _isWeaponReadyGroup = new bool[count];
            return;
        }

        for (int i = 0; i < count; i++)
        {
            WeaponUsage weaponUsage = WeaponUsages[i];

            if (weaponUsage.Weapon.IsValid && weaponUsage.AttackDistance >=
                Vector3.Distance(transform.position, CurrentEnemy.transform.position))
            {
                IsWeaponReadyGroup[i] = true;
                continue;
            }

            IsWeaponReadyGroup[i] = false;

            //TODO: 防空
        }
    }

    public void SetCurrentEnemy(Damageable enemy)
    {
        _currentEnemy = enemy;
    }

    public void RemoveCurrentEnemy()
    {
        _currentEnemy = null;
    }

    public bool TryGetEnemy(FactionType alliance, out Damageable enemy)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _maxAttackDistance, 1 << _layer);
        enemy = null;
        if (colliders == null)
            return false;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].TryGetComponent(out Damageable target))
                if (!target.IsDead
                    && target.Faction != alliance
                    && target.Faction != FactionType.None 
                    && target.Controllable.IsVisibleByLocalPlayer
                    && _maxAttackDistance >= Vector3.Distance(target.transform.position, transform.position))
                {
                    enemy = target;
                    return true;
                }
        }

        return false;
    }

    /// <summary>
    /// 提供一个方法让状态机判断敌人是否在战斗范围内
    /// </summary>
    /// <returns></returns>
    public bool IsTargetInRange()
    {
        return _currentEnemy != null &&
               _maxAttackDistance >= Vector3.Distance(transform.position, _currentEnemy.transform.position);
    }

    private WeaponUsage[] GetInitialWeapons(WeaponItem[] weaponItems)
    {
        WeaponUsage[] weaponUsages = GetWeaponConfigs(weaponItems);
        _isWeaponReadyGroup = new bool[weaponUsages.Length];

        for (int i = 0; i < weaponItems.Length; i++)
        {
            if (!_isAntiAircraft)
                _isAntiAircraft = weaponUsages[i].IsAntiAircraft || _isAntiAircraft;

            _maxAttackDistance = weaponUsages[i].AttackDistance > _maxAttackDistance
                ? weaponUsages[i].AttackDistance
                : _maxAttackDistance;
            _isWeaponReadyGroup[i] = true;
        }

        _chaseDistance = weaponUsages[0].AttackDistance;

        return weaponUsages;
    }

    private WeaponUsage[] GetWeaponConfigs(WeaponItem[] weaponConfigItems)
    {
        int count = weaponConfigItems.Length;
        WeaponUsage[] weaponUsages = new WeaponUsage[count];

        for (int i = 0; i < count; i++)
        {
            WeaponSO config = weaponConfigItems[i].WeaponSO;
            weaponUsages[i] = new WeaponUsage(config.GetWeaponConfig(this, weaponConfigItems[i].WeaponAnchor),
                config.AttackDistance, config.IsAntiAircraft);
        }

        return weaponUsages;
    }

    [Serializable]
    public struct WeaponItem
    {
        public WeaponSO WeaponSO;
        public Transform WeaponAnchor;
    }
}

public class WeaponUsage
{
    private readonly Weapon _weapon = default;
    private readonly float _attackDistance = default;
    private readonly bool _isAntiAircraft = default;

    public Weapon Weapon => _weapon;
    public float AttackDistance => _attackDistance;
    public bool IsAntiAircraft => _isAntiAircraft;

    public WeaponUsage(Weapon weapon, float attackDistance, bool isAntiAircraft)
    {
        _weapon = weapon;
        _attackDistance = attackDistance;
        _isAntiAircraft = isAntiAircraft;
    }
}