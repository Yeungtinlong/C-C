using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Load Event Channel")]
public class LoadEventChannelSO : EventChannelBaseSO
{
    public UnityAction<GameSceneSO, bool, bool> OnLoadingRequested;

    public void RaiseEvent(GameSceneSO sceneToLoad, bool isShowLoadingScreen = false, bool isFadeScreen = false)
    {
        if (OnLoadingRequested != null)
        {
            OnLoadingRequested.Invoke(sceneToLoad, isShowLoadingScreen, isFadeScreen);
        }
        else
        {
            Debug.LogWarning("正在发起一次场景加载，但该事件没有被监听。");
        }
    }
}
