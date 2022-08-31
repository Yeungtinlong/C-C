using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/UI/Order Ring Channel", fileName = "OrderRingChannel")]
public class OrderRingChannelSO : ScriptableObject
{
    public UnityAction<Vector3, Quaternion> OnOrderRingRequested;

    public void RaiseEvent(Vector3 position, Quaternion rotation)
    {
        if (OnOrderRingRequested != null)
        {
            OnOrderRingRequested.Invoke(position, rotation);
        }
    }
}
