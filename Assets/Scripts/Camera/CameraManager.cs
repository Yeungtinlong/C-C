using Cinemachine;
using System.Collections;
using CNC.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineFreeLook _freeLookCamera = default;
    [SerializeField] private InputReader _inputReader = default;
    [SerializeField] private CameraControlConfigSO _cameraControlConfigSO = default;
    [SerializeField] private LevelLimitSO _levelLimitSO = default;
    [SerializeField] private bool _canMouseMoveCamera = true;
    [SerializeField] private bool _canMoveCamera = true;

    private Transform _transform;

    // It will be true when you move the camera by holding on keyboard arrows.
    private bool _isHoldingArrow = false;

    // It will be used together by mouse and keyboard.
    private Vector3 _cameraMoveVector = Vector3.zero;
    private bool _isMouseMovingCamera = false;

    private float _panSpeed;
    private float _rotateSpeed;
    private int _maxRotateTimes;
    private float _cameraMaxSpeed;
    private float _timeToFullSpeed;
    private float _timeToStop;
    private float _startAcceleration;
    private float _brakeAcceleration;
    private float _currentSpeed;

    private Vector3 _lastOffset;

    private int _currentRotateTimes = 0;
    private bool _canReceivedRotate = true;

    private const float BRAKE_THRESHOLD = 0.05f;

    private void Awake()
    {
        _transform = transform;
        SetupCameraConfig();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        // Disable the moving camera by mouse in editor mode, is easier to debugging.
        _canMouseMoveCamera = true;
#endif
        _inputReader.zoomCameraEvent += OnZoomCamera;
        _inputReader.rotateCameraEvent += OnRotateCamera;
        _inputReader.resetCameraRotation += OnResetCameraRotation;
        _inputReader.moveCameraEvent += OnKeyBoardMoveCamera;
        _inputReader.enableSelectionEvent += OnBanMoveCamera;
        _inputReader.disableSelectionEvent += OnAllowMoveCamera;
    }

    private void OnDisable()
    {
        _inputReader.zoomCameraEvent -= OnZoomCamera;
        _inputReader.rotateCameraEvent -= OnRotateCamera;
        _inputReader.resetCameraRotation -= OnResetCameraRotation;
        _inputReader.moveCameraEvent -= OnKeyBoardMoveCamera;
        _inputReader.enableSelectionEvent -= OnBanMoveCamera;
        _inputReader.disableSelectionEvent -= OnAllowMoveCamera;
    }

    private void Update()
    {
        OnMouseMoveCamera(Mouse.current.position.ReadValue());
        MoveCameraOnUpdate();
    }

    private void SetupCameraConfig()
    {
        _panSpeed = _cameraControlConfigSO.PanSpeed;
        _rotateSpeed = _cameraControlConfigSO.RotateSpeed;
        _maxRotateTimes = _cameraControlConfigSO.MaxRotateTimes;
        _cameraMaxSpeed = _cameraControlConfigSO.CameraMaxSpeed;
        _timeToFullSpeed = _cameraControlConfigSO.TimeToFullSpeed;
        _timeToStop = _cameraControlConfigSO.TimeToStop;
        _startAcceleration = _cameraMaxSpeed / _timeToFullSpeed;
        _brakeAcceleration = _cameraMaxSpeed / _timeToStop;
    }

    private void OnKeyBoardMoveCamera(Vector2 direction, bool isHolding)
    {
        _isHoldingArrow = isHolding;

        if (!_isHoldingArrow && !_isMouseMovingCamera)
        {
            _cameraMoveVector = Vector3.zero;
            return;
        }

        _cameraMoveVector = new Vector3(direction.x * _panSpeed, 0f, direction.y * _panSpeed);
    }

    private void OnMouseMoveCamera(Vector2 mousePoint)
    {
        if (!_canMouseMoveCamera)
            return;

        Vector2 direction = Vector2.zero;

        if (mousePoint.x >= Screen.width - 10f)
            direction.x = 1f;
        else if (mousePoint.x <= 10f)
            direction.x = -1f;

        if (mousePoint.y >= Screen.height - 10f)
            direction.y = 1f;
        else if (mousePoint.y <= 10f)
            direction.y = -1f;

        if (direction != Vector2.zero)
        {
            _isMouseMovingCamera = true;
            _cameraMoveVector = new Vector3(direction.x * _panSpeed, 0f, direction.y * _panSpeed);
        }
        else
        {
            _isMouseMovingCamera = false;
            if (!_isHoldingArrow)
                _cameraMoveVector = Vector3.zero;
        }
    }

    private void OnBanMoveCamera(Vector2 position)
    {
        _canMoveCamera = false;
    }

    private void OnAllowMoveCamera(Vector2 position)
    {
        _canMoveCamera = true;
    }

    private void MoveCameraOnUpdate()
    {
        if (!_canMoveCamera)
            return;
        
        LevelRectSO rect = _levelLimitSO.CurrentLevelRect;
        Vector3 cameraSystemPos = _transform.position;
        
        if (_canMouseMoveCamera && _isMouseMovingCamera || _isHoldingArrow)
        {
            Vector3 offset = _transform.forward * _cameraMoveVector.z + _transform.right * _cameraMoveVector.x;
            _lastOffset = offset;
            // s = (vt^2 - v0^2) / 2a;
            float brakeDistance = 0.5f * Utils.Sqr(_currentSpeed) / _brakeAcceleration;
            
            if (_currentSpeed < _cameraMaxSpeed)
            {
                _currentSpeed += Time.deltaTime * _startAcceleration;
            }
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0f, _cameraMaxSpeed);
            Vector3 newPos = cameraSystemPos + offset * _currentSpeed * Time.deltaTime;
            
            newPos.x = Mathf.Clamp(newPos.x, rect.MinX, rect.MaxX);
            newPos.z = Mathf.Clamp(newPos.z, rect.MinZ, rect.MaxZ);

            _transform.position = newPos;
        }
        else if (!_isMouseMovingCamera && !_isHoldingArrow && _currentSpeed > BRAKE_THRESHOLD)
        {
            if (_currentSpeed < BRAKE_THRESHOLD)
            {
                _currentSpeed = 0f;
            }
            
            _currentSpeed -= Time.deltaTime * _brakeAcceleration;
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0f, _cameraMaxSpeed);
            Vector3 newPos = cameraSystemPos + _lastOffset * _currentSpeed * Time.deltaTime;
            
            newPos.x = Mathf.Clamp(newPos.x, rect.MinX, rect.MaxX);
            newPos.z = Mathf.Clamp(newPos.z, rect.MinZ, rect.MaxZ);

            _transform.position = newPos;
        }
    }

    private void OnZoomCamera(float z)
    {
        //Vector3 zoomDirection = (transform.position - _freeLookCamera.transform.position).normalized;
        //_freeLookCamera.transform.position = Vector3.Lerp(_freeLookCamera.transform.position, _freeLookCamera.transform.position + z * zoomDirection * _zoomSpeed, Time.deltaTime);
        //if (Vector3.Distance(_freeLookCamera.transform.position, transform.position) < _zoomInMax)
        //    _freeLookCamera.transform.position = transform.position - zoomDirection * _zoomInMax;
        //else if (Vector3.Distance(_freeLookCamera.transform.position, transform.position) > _zoomOutMax)
        //    _freeLookCamera.transform.position = transform.position - zoomDirection * _zoomOutMax;
        _freeLookCamera.m_YAxis.m_InputAxisValue = -z;
    }

    /// <summary>
    /// When pressing left turn key, input would be 1. When pressing right key, it would be -1.
    /// </summary>
    private void OnRotateCamera(float input)
    {
        if (!_canReceivedRotate)
            return;

        _canReceivedRotate = false;
        StopAllCoroutines();
        StartCoroutine(RotateCameraPerFrame(input > 0));
    }

    private IEnumerator RotateCameraPerFrame(bool isClockwise)
    {
        _currentRotateTimes += isClockwise ? 1 : -1;
        _currentRotateTimes = _currentRotateTimes % _maxRotateTimes;
        float targetAngle = _currentRotateTimes * 360f / _maxRotateTimes;
        Quaternion targetRotaion = Quaternion.Euler(new Vector3(0f, targetAngle, 0f));

        while (true)
        {
            float remainingAngle = Quaternion.Angle(transform.rotation, targetRotaion);
            if (remainingAngle < 360f / _maxRotateTimes / 2)
                _canReceivedRotate = true;

            if (Quaternion.Angle(transform.rotation, targetRotaion) <= 0.1f)
            {
                transform.rotation = Quaternion.Euler(new Vector3(0f, targetAngle, 0f));
                yield break;
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotaion, Time.deltaTime * _rotateSpeed);
            yield return null;
        }
    }

    private void OnResetCameraRotation()
    {
        if (!_canReceivedRotate)
            return;

        _canReceivedRotate = false;
        _currentRotateTimes = 0;
        StopAllCoroutines();
        StartCoroutine(ResetCameraRotationPerFrame());
    }

    private IEnumerator ResetCameraRotationPerFrame()
    {
        Quaternion targetRotation = Quaternion.Euler(Vector3.zero);
        while (true)
        {
            float remainingAngle = Quaternion.Angle(transform.rotation, targetRotation);
            if (remainingAngle < 360f / _maxRotateTimes / 2)
                _canReceivedRotate = true;

            if (remainingAngle <= 0.1f)
            {
                transform.rotation = targetRotation;
                yield break;
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * _rotateSpeed);
            yield return null;
        }
    }
}