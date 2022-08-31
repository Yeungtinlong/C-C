using System.Collections;
using System.Collections.Generic;
using CNC.Factory;
using CNC.Pool;
using UnityEngine;

[CreateAssetMenu(menuName = "Factory/OrderRing Factory")]
public class OrderRingFactorySO : FactorySO<OrderRing>
{
    [SerializeField] private OrderRing _orderRing = default;
    public override OrderRing Create()
    {
        return Instantiate(_orderRing);
    }
}
