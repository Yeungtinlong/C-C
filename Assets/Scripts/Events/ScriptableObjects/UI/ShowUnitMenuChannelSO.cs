using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/UI/Show Unit Menu Channel", fileName = "ShowUnitMenuChannelSO")]
public class ShowUnitMenuChannelSO : ScriptableObject
{
    public event UnityAction<bool> OnShowUnitMenuRequested;

    public void RaiseEvent(bool isShow)
    {
        if (OnShowUnitMenuRequested != null)
        {
            OnShowUnitMenuRequested.Invoke(isShow);
        }
    }
}
