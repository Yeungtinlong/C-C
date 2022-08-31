using CNC.StateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CNC.StateMachine
{
    public abstract class WeaponSO : ScriptableObject
    {
        [SerializeField] private int _attackDistance = default;
        [SerializeField] private float _reloadDuration = default;
        [SerializeField] private bool _isAntiAircraft = default;
        public int AttackDistance => _attackDistance;
        public float ReloadDuration => _reloadDuration;
        public bool IsAntiAircraft => _isAntiAircraft;

        public Weapon GetWeaponConfig(Attacker attacker, Transform weaponAnchor)
        {
            Weapon weapon = CreateWeapon();
            weapon._originSO = this;
            weapon.OnAwake(attacker, weaponAnchor);
            return weapon;
        }

        protected abstract Weapon CreateWeapon();
    }

    public abstract class Weapon
    {
        internal WeaponSO _originSO;
        protected WeaponSO OriginSO => _originSO;

        public bool IsValid { get; set; } = true;

        public virtual void OnAwake(Attacker attacker, Transform weaponAnchor) { }
        public virtual void Attack() { }
        public virtual IEnumerator Reload()
        {
            yield return new WaitForSeconds(_originSO.ReloadDuration);
            IsValid = true;
        }
    }
}
