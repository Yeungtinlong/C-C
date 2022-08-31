using System;
using CNC.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public abstract class Projectile : MonoBehaviour
{
    [SerializeField] protected TrailRenderer _trail = default;
    [SerializeField] protected ParticleSystem _particle = default;
    [SerializeField] protected GameObject _renderer = default;

    protected Transform _transform;
    protected float _maxSpeed;
    protected float _timeToFullSpeed;

    protected int _damageToHuman;
    protected int _damageToVehicle;
    protected int _damageToConstruction;
    protected ProjectileType _projectileType;
    protected float _currentSpeed;
    protected Damageable _target;
    protected Vector3 _targetOriginPosition;
    protected Vector3 _launchOriginPosition;
    protected bool _canHitOnWay;
    protected Attacker _ownerAttacker;

    public int DamageToHuman
    {
        get => _damageToHuman;
        set => _damageToHuman = value;
    }

    public int DamageToVehicle
    {
        get => _damageToVehicle;
        set => _damageToVehicle = value;
    }

    public int DamageToConstruction
    {
        get => _damageToConstruction;
        set => _damageToConstruction = value;
    }

    public ProjectileType ProjectileType
    {
        get => _projectileType;
        set => _projectileType = value;
    }

    public float MaxSpeed
    {
        get => _maxSpeed;
        set => _maxSpeed = value;
    }

    public float CurrentSpeed
    {
        get => _currentSpeed;
        set => _currentSpeed = value;
    }

    public float TimeToFullSpeed
    {
        get => _timeToFullSpeed;
        set => _timeToFullSpeed = value;
    }

    public Damageable Target
    {
        get => _target;
        set => _target = value;
    }

    public Vector3 TargetOriginPosition
    {
        get => _targetOriginPosition;
        set => _targetOriginPosition = value;
    }

    public Attacker OwnerAttacker
    {
        get => _ownerAttacker;
        set => _ownerAttacker = value;
    }

    public bool CanHitOnWay
    {
        get => _canHitOnWay;
        set => _canHitOnWay = value;
    }

    public float Acceleration
    {
        get
        {
            if (_timeToFullSpeed == 0f)
                return 0f;

            return _maxSpeed / _timeToFullSpeed;
        }
    }

    public bool IsValid { get; set; }
    public UnityAction<Projectile> OnHitTarget;

    public virtual void InitializeProjectile(Projectile projectile, Transform launcher, Damageable target, Attacker owner)
    {
        _transform = transform;

        _damageToHuman = projectile.DamageToHuman;
        _damageToVehicle = projectile.DamageToVehicle;
        _damageToConstruction = projectile.DamageToConstruction;
        _projectileType = projectile.ProjectileType;
        _maxSpeed = projectile.MaxSpeed;
        _timeToFullSpeed = projectile.TimeToFullSpeed;
        _canHitOnWay = projectile.CanHitOnWay;
        
        _target = target;
        _ownerAttacker = owner;
        _targetOriginPosition = target.transform.position;
        _launchOriginPosition = launcher.position;

        _transform.position = launcher.position;
        _transform.rotation = launcher.rotation;
        
        _currentSpeed = 0f;
        IsValid = true;

        PlayEffects();
        StartCoroutine(Fly());
    }

    protected abstract IEnumerator Fly();

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!IsValid)
            return;

        if (other.TryGetComponent(out Damageable damageable))
        {
            if (_canHitOnWay || damageable == _target)
            {
                IsValid = false;

                StopEffects();
                StartCoroutine(RecycleAfterEffectFinished());
                
                // Debug.Log(_projectileType + " from " + _ownerAttacker.gameObject + " hit " + damageable.gameObject);

                if (damageable.Faction != _ownerAttacker.OwnerDamageable.Faction)
                {
                    MakeDamage(damageable);
                }
            }
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            IsValid = false;

            StopEffects();
            StartCoroutine(RecycleAfterEffectFinished());
        }
    }

    protected virtual void PlayEffects()
    {
        _renderer.SetActive(true);
        _trail.emitting = true;
        _trail.Clear();
        _particle?.Play();
    }

    protected virtual void StopEffects()
    {
        _renderer.SetActive(false);
        _trail.emitting = false;
        _particle?.Stop();
    }

    protected virtual IEnumerator RecycleAfterEffectFinished()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            OnHitTarget.Invoke(this);
            yield break;
        }
    }

    protected void MakeDamage(Damageable target)
    {
        switch (target.DamageableType)
        {
            case DamageableType.Human:
                target.TakeDamage(_damageToHuman);
                break;
            case DamageableType.Vehicle:
                target.TakeDamage(_damageToVehicle);
                break;
            case DamageableType.Construction:
                target.TakeDamage(_damageToConstruction);
                break;
            default:
                break;
        }
    }
}

public enum ProjectileType
{
    Shell,
    Missile,
    Laser,
    GD2Bullet
}