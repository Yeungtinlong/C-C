using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderRingManager : MonoBehaviour
{
    [SerializeField] private OrderRingPoolSO _orderRingPool = default;
    [SerializeField] private int _preWarmCount = 5;

    [Header("Listening to")] [SerializeField]
    private OrderRingChannelSO _orderRingChannel = default;

    private void Start()
    {
        PreWarm();
    }

    private void OnEnable()
    {
        _orderRingChannel.OnOrderRingRequested += PlaceOrderRing;
    }

    private void OnDisable()
    {
        _orderRingChannel.OnOrderRingRequested -= PlaceOrderRing;
    }

    private void PreWarm()
    {
        _orderRingPool.Prewarm(_preWarmCount);
    }

    private OrderRing GetOrderRing()
    {
        OrderRing orderRing = _orderRingPool.Pop();
        orderRing.OnRelease += ReleaseOrderRing;
        return orderRing;
    }

    private void ReleaseOrderRing(OrderRing orderRing)
    {
        orderRing.OnRelease -= ReleaseOrderRing;
        _orderRingPool.Push(orderRing);
    }

    private void PlaceOrderRing(Vector3 position, Quaternion rotation)
    {
        OrderRing orderRing = GetOrderRing();
        orderRing.transform.position = position;
        orderRing.transform.rotation = rotation;
    }
}