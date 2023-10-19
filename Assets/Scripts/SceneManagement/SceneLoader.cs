using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private GameSceneSO _gameplayScene = default;

    [Header("Listening to")]
    [SerializeField] private LoadEventChannelSO _loadLevelEvent = default;
    //[SerializeField] private LoadEventChannelSO _loadMenuEvent = default;
    [SerializeField] private LoadEventChannelSO _coldStartupLevelEvent = default;
    [SerializeField] private FadeChannelSO _fadeRequestChannel = default;

    [Header("Broadcasting on")]
    [SerializeField] private VoidEventChannelSO _onSceneReady = default;

    private AsyncOperationHandle<SceneInstance> _gameplayManagerLoadingOpHandle;
    private AsyncOperationHandle<SceneInstance> _loadingOperationHandle;

    private GameSceneSO _sceneToLoad;
    private GameSceneSO _currentlyLoadedScene;
    private float _fadeDuration = 0.5f;
    private bool _isShowLoadingScreen;

    // Scene的封装，用于异步操作
    private SceneInstance _gameplayManagerSceneInstance = new SceneInstance();


    private bool _isLoading = false;

    private void OnEnable()
    {
        _loadLevelEvent.OnLoadingRequested += LoadLevel;

#if UNITY_EDITOR
        _coldStartupLevelEvent.OnLoadingRequested += LevelColdStartup;
#endif
    }

    private void OnDisable()
    {
        _loadLevelEvent.OnLoadingRequested -= LoadLevel;

#if UNITY_EDITOR
        _coldStartupLevelEvent.OnLoadingRequested -= LevelColdStartup;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 这里的冷启动是特指直接从关卡的界面点Play按钮运行，这时需要加载Gameplay场景才能正常游戏。如果是通过主菜单加载游戏就不算冷启动。
    /// </summary>
    private void LevelColdStartup(GameSceneSO currentOpenedScene, bool isShowLoadingScreen, bool isFadeScreen)
    {
        _currentlyLoadedScene = currentOpenedScene;

        if (_gameplayManagerSceneInstance.Scene == null || !_gameplayManagerSceneInstance.Scene.isLoaded)
        {
            _gameplayManagerLoadingOpHandle = _gameplayScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
            _gameplayManagerLoadingOpHandle.Completed += OnGameplayManagersLoaded;

            StartGameplay();
        }
    }
#endif

    /// <summary>
    /// 正常加载场景（非冷启动），则判定是否有Gameplay场景，没有的话就加载
    /// </summary>
    private void LoadLevel(GameSceneSO levelToLoad, bool isShowLoadingScreen, bool isFadeScreen)
    {
        if (_isLoading)
            return;

        _sceneToLoad = levelToLoad;
        _isShowLoadingScreen = isShowLoadingScreen;
        _isLoading = true;

        // 如果还没有Gameplay场景，就需要先加载Gamplay再加载Level（第一次按开始游戏进入时）
        if (_gameplayManagerSceneInstance.Scene == null || !_gameplayManagerSceneInstance.Scene.isLoaded)
        {
            _gameplayManagerLoadingOpHandle = _sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
            _gameplayManagerLoadingOpHandle.WaitForCompletion();
            _gameplayManagerSceneInstance = _gameplayManagerLoadingOpHandle.Result;
        }

        StartCoroutine(UnloadPreviousScene());
    }

    private void OnGameplayManagersLoaded(AsyncOperationHandle<SceneInstance> handle)
    {
        _gameplayManagerSceneInstance = _gameplayManagerLoadingOpHandle.Result;
    }


    private void LoadMenu()
    {

    }

    private IEnumerator UnloadPreviousScene()
    {

        _fadeRequestChannel.FadeOut(_fadeDuration);

        yield return new WaitForSeconds(_fadeDuration);

        if (_currentlyLoadedScene != null) // 若正常初始化进入游戏，然后点击开始菜单加入游戏的话，则_currentlyLoadedScene为null
        {
            Debug.Log(_currentlyLoadedScene.name);
            if (_currentlyLoadedScene.sceneReference.OperationHandle.IsValid())
            {
                _currentlyLoadedScene.sceneReference.UnLoadScene();
                Debug.Log("Unload");
            }
#if UNITY_EDITOR
            else
            {
                // 当冷启动后，玩家切换到新的场景。
                // 此时，场景加载的异步handle并未被使用过。
                // 故使用SceneManager代替Addressable的卸载场景方案。
                SceneManager.UnloadSceneAsync(_currentlyLoadedScene.sceneReference.editorAsset.name);
            }
#endif
        }

        LoadNewScene();
    }

    private void LoadNewScene()
    {
        _loadingOperationHandle = _sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true, 0);
        _loadingOperationHandle.Completed += OnNewSceneLoaded;
    }

    private void OnNewSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _currentlyLoadedScene = _sceneToLoad;

        Scene s = obj.Result.Scene;
        SceneManager.SetActiveScene(s);

        _isLoading = false;

        _fadeRequestChannel.FadeIn(_fadeDuration);

        StartGameplay();
    }

    private void StartGameplay()
    {
        _onSceneReady.RaiseEvent(); // 场景准备完毕，播放过场动画或开始游戏
    }

    private void ExitGame()
    {
        Application.Quit();
    }
}
