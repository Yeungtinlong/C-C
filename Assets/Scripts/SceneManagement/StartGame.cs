using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 这个类包含了开始游戏按钮按下时要调用的方法
/// </summary>
public class StartGame : MonoBehaviour
{
    [SerializeField]
    private GameSceneSO _levelsToLoad;
    [SerializeField]
    private bool _isShowLoadScreen;
    [SerializeField]
    private SaveSystem _saveSystem = default;

    [Header("Listening to")]
    [SerializeField]
    private VoidEventChannelSO _startNewGameEvent = default;

    [Header("Broadcasting on")]
    [SerializeField]
    private LoadEventChannelSO _startGameEvent = default;

    //private bool _hasSaveData;
    private void Start()
    {
        _startNewGameEvent.OnEventRaised += StartNewGame;
    }

    private void OnDestroy()
    {
        _startNewGameEvent.OnEventRaised -= StartNewGame;
    }

    private void StartNewGame()
    {
        //_hasSaveData = false;

        _saveSystem.WriteEmptySaveFile();
        _saveSystem.SetNewGameData();

        _startGameEvent.RaiseEvent(_levelsToLoad, _isShowLoadScreen); // 开始游戏
    }
}
