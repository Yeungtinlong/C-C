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
        yield return new WaitForSeconds(0.4f); // �ȴ����г����������
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
            // TODO: ѯ���Ƿ�����浵��ʼ����Ϸ
        }
        
    }

    private void ConfirmStartNewGame()
    {
        _startNewGameEvent.RaiseEvent();
    }
}
