using System.Collections;
using System.Collections.Generic;
using CNC.Factory;
using CNC.Pool;
using UnityEngine;

[CreateAssetMenu(menuName = "Pool/OrderRing Pool")]
public class OrderRingPoolSO : ComponentPoolSO<OrderRing>
{
    [SerializeField] private OrderRingFactorySO _factory = default;
    public override IFactory<OrderRing> Factory
    {
        get => _factory;
        set => _factory = value as OrderRingFactorySO;
    }
}
