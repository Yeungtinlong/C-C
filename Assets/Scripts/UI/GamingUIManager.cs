using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamingUIManager : MonoBehaviour
{
    [SerializeField] private UnitCommandMenu _unitCommandMenu = default;
    
    [Header("Listening to")]
    [SerializeField] private ShowUnitMenuChannelSO _showUnitMenuChannelSO = default;

    private void OnEnable()
    {
        _showUnitMenuChannelSO.OnShowUnitMenuRequested += ShowUnitCommandMenu;
    }
    
    private void OnDisable()
    {
        _showUnitMenuChannelSO.OnShowUnitMenuRequested -= ShowUnitCommandMenu;
    }

    private void ShowUnitCommandMenu(bool isShow)
    {
        _unitCommandMenu.gameObject.SetActive(isShow);
    }
}
