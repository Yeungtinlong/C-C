using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/UI/Change Outline Color Channel", fileName = "ChangeOutlineColorChannelSO")]
public class ChangeOutlineColorChannelSO : ScriptableObject
{
    public UnityAction<UnitAlignment> OnChangeOutlineColor;

    public void RaiseEvent(UnitAlignment alignment)
    {
        if (OnChangeOutlineColor != null)
        {
            OnChangeOutlineColor.Invoke(alignment);
        }
    }
}