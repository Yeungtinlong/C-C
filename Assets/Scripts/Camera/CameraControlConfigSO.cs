using UnityEngine;

[CreateAssetMenu(fileName = "CameraControlConfig", menuName = "Game/Camera Control Config")]
public class CameraControlConfigSO : ScriptableObject
{
    [SerializeField] private float _panSpeed = default;
    [SerializeField] private float _rotateSpeed = default;
    [SerializeField] private int _maxRotateTimes = default;
    [SerializeField] private float _cameraMaxSpeed = default;
    [SerializeField] private float _timeToFullSpeed = default;
    [SerializeField] private float _timeToStop = default;

    public float PanSpeed => _panSpeed;
    public float RotateSpeed => _rotateSpeed;
    public int MaxRotateTimes => _maxRotateTimes;
    public float CameraMaxSpeed => _cameraMaxSpeed;
    public float TimeToFullSpeed => _timeToFullSpeed;
    public float TimeToStop => _timeToStop;
}
