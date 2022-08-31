using CNC.StateMachine;
using UnityEngine;

[CreateAssetMenu(menuName = "Entity Config/Weapons/Projectile Weapon")]
public class ProjectileWeaponSO : WeaponSO
{
    [SerializeField] private ProjectileSO _projectile = default;
    [Header("Broadcasting on")]
    [SerializeField] private ProjectileEventChannelSO _projectileRequestChannel = default;

    public ProjectileSO ProjectileSO => _projectile;
    public ProjectileEventChannelSO ProjectileRequestChannel => _projectileRequestChannel;

    protected override Weapon CreateWeapon() => new ProjectileWeapon();
}

public class ProjectileWeapon : Weapon
{
    private ProjectileWeaponSO _configSO => base.OriginSO as ProjectileWeaponSO;

    private Transform _weaponAnchor;
    private Attacker _attacker;
    private Projectile _projectilePrefab;

    public override void OnAwake(Attacker attacker, Transform weaponAnchor)
    {
        _attacker = attacker;
        _weaponAnchor = weaponAnchor;
        _projectilePrefab = _configSO.ProjectileSO.GetProjectilePrefab();
    }

    public override void Attack()
    {
        _configSO.ProjectileRequestChannel.RaiseEvent(_projectilePrefab, _weaponAnchor, _attacker.CurrentEnemy, _attacker);
    }
}