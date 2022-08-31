using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSO", menuName = "Entity Config/Projectile")]
public class ProjectileSO : ScriptableObject
{
    [SerializeField] private ProjectileType _projectileType = default;
    [SerializeField] private AttackConfigSO _attackConfig = default;
    [SerializeField] private float _maxSpeed = default;
    [SerializeField] private float _timeToFullSpeed = default;
    [SerializeField] private Projectile _projectilePrefab = default;
    [SerializeField] private bool _canHitOnWay = default;

    public Projectile GetProjectilePrefab()
    {
        _projectilePrefab.DamageToHuman = _attackConfig.DamageToHuman;
        _projectilePrefab.DamageToVehicle = _attackConfig.DamageToVehicle;
        _projectilePrefab.DamageToConstruction = _attackConfig.DamageToConstruction;
        _projectilePrefab.ProjectileType = _projectileType;
        _projectilePrefab.MaxSpeed = _maxSpeed;
        _projectilePrefab.TimeToFullSpeed = _timeToFullSpeed;
        _projectilePrefab.CanHitOnWay = _canHitOnWay;

        return _projectilePrefab;
    }

    // public void SetProjectile(ProjectileType projectileType, AttackConfigSO attackConfig, float maxSpeed, float timeToFullSpeed, Projectile projectilePrefab)
    // {
    //     _projectileType = projectileType;
    //     _attackConfig = attackConfig;
    //     _maxSpeed = maxSpeed;
    //     _timeToFullSpeed = timeToFullSpeed;
    //     _projectilePrefab = projectilePrefab;
    // }
}