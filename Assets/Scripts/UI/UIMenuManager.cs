using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField] private UIMainMenu _mainMenuPanel = default;

    [Header("Broadcasting on")]
    [SerializeField]
    private VoidEventChannelSO _startNewGameEvent = default;

    private bool _hasSaveData;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.4f); // 等待所有场景加载完成
        SetMenuScreen();
    }

    private void SetMenuScreen()
    {
        _mainMenuPanel.NewGameButtonAction += ButtonStartNewGameClicked;
    }

    private void ButtonStartNewGameClicked()
    {
        if (!_hasSaveData)
        {
            ConfirmStartNewGame();
        }
        else
        {
            // TODO: 询问是否放弃存档开始新游戏
        }
        
    }

    private void ConfirmStartNewGame()
    {
        _startNewGameEvent.RaiseEvent();
    }
}
