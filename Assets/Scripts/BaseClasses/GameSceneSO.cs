using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// �������������͵���Ϸ�����Ļ����࣬�磨Levels, Menus, Managers��
/// </summary>
public class GameSceneSO : DescriptionBaseSO
{
    public GameSceneType sceneType;
    public AssetReference sceneReference;

    public enum GameSceneType
    {
        // �ɲ����ĳ���
        Level,
        Menu,

        // ���ⳡ��
        Initialization,
        PersistentManagers,
        Gameplay
    }
}
