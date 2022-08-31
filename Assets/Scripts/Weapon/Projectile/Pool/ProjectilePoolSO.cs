using CNC.Factory;
using CNC.Pool;
using UnityEngine;

[CreateAssetMenu(menuName = "Pool/Projectile Pool")]
public class ProjectilePoolSO : ComponentPoolSO<Projectile>
{
    [SerializeField] private ProjectileFactorySO _factory;
    [SerializeField] private ProjectileType _projectileType;

    public override IFactory<Projectile> Factory
    {
        get
        {
            return _factory;
        }
        set
        {
            _factory = value as ProjectileFactorySO;
        }
    }

    public ProjectileType ProjectileType => _projectileType;
}
