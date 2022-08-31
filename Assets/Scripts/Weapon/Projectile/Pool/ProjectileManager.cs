using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CNC.StateMachine;

public class ProjectileManager : MonoBehaviour
{
    [Header("Projectiles pools")]
    [SerializeField] private ProjectilePoolSO[] _pools = default;
    [SerializeField] private int _initialSize = default;

    [Header("Listening to")]
    [SerializeField] private ProjectileEventChannelSO _projectileRequestChannel = default;

    private Dictionary<ProjectileType, ProjectilePoolSO> _projectilePools = default;

    private void Awake()
    {
        _projectilePools = InitializePools(_pools);
    }

    private void OnEnable()
    {
        _projectileRequestChannel.OnEventRaised += RequestProjectile;
    }

    private void OnDisable()
    {
        _projectileRequestChannel.OnEventRaised -= RequestProjectile;
    }

    private Dictionary<ProjectileType, ProjectilePoolSO> InitializePools(ProjectilePoolSO[] pools)
    {
        Dictionary<ProjectileType, ProjectilePoolSO> dic = new Dictionary<ProjectileType, ProjectilePoolSO>();

        for (int i = 0; i < pools.Length; i++)
        {
            ProjectilePoolSO pool = pools[i];
            pool.Prewarm(_initialSize);
            dic.Add(pool.ProjectileType, pool);
        }

        return dic;
    }

    private void RequestProjectile(Projectile projectilePrefab, Transform launcher, Damageable target, Attacker owner)
    {
        Projectile pjt = _projectilePools[projectilePrefab.ProjectileType].Pop();
        pjt.InitializeProjectile(projectilePrefab, launcher, target, owner);
        pjt.OnHitTarget += ReturnProjectile;
    }

    private void ReturnProjectile(Projectile projectile)
    {
        projectile.OnHitTarget -= ReturnProjectile;
        _projectilePools[projectile.ProjectileType].Push(projectile);
    }
}
