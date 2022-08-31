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

    // Scene�ķ�װ�������첽����
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
    /// ���������������ֱָ�Ӵӹؿ��Ľ����Play��ť���У���ʱ��Ҫ����Gameplay��������������Ϸ�������ͨ�����˵�������Ϸ�Ͳ�����������
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
    /// �������س��������������������ж��Ƿ���Gameplay������û�еĻ��ͼ���
    /// </summary>
    private void LoadLevel(GameSceneSO levelToLoad, bool isShowLoadingScreen, bool isFadeScreen)
    {
        if (_isLoading)
            return;

        _sceneToLoad = levelToLoad;
        _isShowLoadingScreen = isShowLoadingScreen;
        _isLoading = true;

        // �����û��Gameplay����������Ҫ�ȼ���Gamplay�ټ���Level����һ�ΰ���ʼ��Ϸ����ʱ��
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

        if (_currentlyLoadedScene != null) // ��������ʼ��������Ϸ��Ȼ������ʼ�˵�������Ϸ�Ļ�����_currentlyLoadedSceneΪnull
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
                // ��������������л����µĳ�����
                // ��ʱ���������ص��첽handle��δ��ʹ�ù���
                // ��ʹ��SceneManager����Addressable��ж�س���������
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
        _onSceneReady.RaiseEvent(); // ����׼����ϣ����Ź���������ʼ��Ϸ
    }

    private void ExitGame()
    {
        Application.Quit();
    }
}
