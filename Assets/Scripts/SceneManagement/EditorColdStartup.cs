using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class EditorColdStartup : MonoBehaviour
{
#if UNITY_EDITOR

    [SerializeField] private GameSceneSO _thisSceneSO = default;
    [SerializeField] private GameSceneSO _persistentManagersSO = default;
    [SerializeField] private AssetReference _notifyColdStartupChannel = default;

    private bool _isColdStart = false;

    private void Awake()
    {
        // 如果PersistentManagers场景未加载，则判定为冷启动
        if (!SceneManager.GetSceneByName(_persistentManagersSO.sceneReference.editorAsset.name).isLoaded)
        {
            _isColdStart = true;


        }
    }

    private void Start()
    {
        if (_isColdStart)
        {
            _persistentManagersSO.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true).Completed += LoadEventChannel;
        }

    }

    private void LoadEventChannel(AsyncOperationHandle<SceneInstance> obj)
    {
        _notifyColdStartupChannel.LoadAssetAsync<LoadEventChannelSO>().Completed += OnNotifyChannelLoaded;
    }

    private void OnNotifyChannelLoaded(AsyncOperationHandle<LoadEventChannelSO> obj)
    {
        if (_thisSceneSO != null)
        {
            obj.Result.RaiseEvent(_thisSceneSO);
        }

    }
#endif
}
