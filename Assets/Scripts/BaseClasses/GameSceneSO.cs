using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 此类是所有类型的游戏场景的基础类，如（Levels, Menus, Managers）
/// </summary>
public class GameSceneSO : DescriptionBaseSO
{
    public GameSceneType sceneType;
    public AssetReference sceneReference;

    public enum GameSceneType
    {
        // 可操作的场景
        Level,
        Menu,

        // 特殊场景
        Initialization,
        PersistentManagers,
        Gameplay
    }
}
