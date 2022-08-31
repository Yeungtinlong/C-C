using CNC.Factory;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileFactorySO", menuName = "Factory/Projectile Factory")]
public class ProjectileFactorySO : FactorySO<Projectile>
{
    [SerializeField] private Projectile _prefab = default;
    public Projectile ProjectilePrefab => _prefab;

    public override Projectile Create()
    {
        return Instantiate(_prefab);
    }
}
