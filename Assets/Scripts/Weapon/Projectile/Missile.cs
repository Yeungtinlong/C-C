using System.Collections;
using UnityEngine;

public class Missile : Projectile
{
    private float _turnRaidus = 10f;
    private Vector3 _lastTargetPos;

    public override void InitializeProjectile(Projectile projectile, Transform launcher, Damageable target, Attacker owner)
    {
        base.InitializeProjectile(projectile, launcher, target, owner);
        _lastTargetPos = _targetOriginPosition;
    }

    protected override IEnumerator Fly()
    {
        // 迟一帧计算位置，避免特效错位。
        yield return null;
        
        while (true)
        {
            Vector3 targetPos = _target != null ? _target.transform.position : _lastTargetPos;
            Vector3 dir = Vector3.MoveTowards(_transform.forward, (targetPos - _transform.position).normalized, _currentSpeed / _turnRaidus * Time.deltaTime);
            float distance = _currentSpeed * Time.deltaTime;
            _transform.position += dir * distance;
            _transform.forward = dir;

            if (_currentSpeed < _maxSpeed)
                _currentSpeed += Time.deltaTime * Acceleration;
            else
                _currentSpeed = _maxSpeed;

            yield return null;
        }
    }
}