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
            Debug.LogWarning("���ڷ���һ�γ������أ������¼�û�б�������");
        }
    }
}
