using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, GameInput.IGameplayActions
{
    // Mouse selection events
    public event UnityAction<Vector2> enableSelectionEvent = delegate { };
    public event UnityAction<Vector2> disableSelectionEvent = delegate { };
    public event UnityAction<Vector2> mouseMoveEvent = delegate { };

    public event UnityAction<bool> additionalSelectEvent = delegate { };

    // Mouse make command events
    public event UnityAction<Vector2> makeCommandEvent = delegate { };

    // Mouse zoom screen events
    public event UnityAction<Vector2, bool> moveCameraEvent = delegate { };
    public event UnityAction<float> zoomCameraEvent = delegate { };
    public event UnityAction<float> rotateCameraEvent = delegate { };
    public event UnityAction resetCameraRotation = delegate { };
    public event UnityAction stopCommand = delegate { };
    public event UnityAction layOrStandCommand = delegate { };
    public event UnityAction placePlatoonGhost = delegate { };
    public event UnityAction cancelPlatoonGhost = delegate { };

    private GameInput _gameInput;

    private void OnEnable()
    {
        if (_gameInput == null)
        {
            _gameInput = new GameInput();
            _gameInput.Gameplay.SetCallbacks(this);
        }

        EnableGameplayInput();
    }

    private void OnDisable()
    {
        DisableAllInput();
    }

    //public void OnPanCamera(InputAction.CallbackContext context) { }

    public void OnZoomCamera(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            zoomCameraEvent.Invoke(context.ReadValue<float>());
    }

    public void OnMouseControlSelect(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            enableSelectionEvent.Invoke(Mouse.current.position.ReadValue());
        else if (context.phase == InputActionPhase.Canceled)
            disableSelectionEvent.Invoke(Mouse.current.position.ReadValue());
    }

    public void OnMouseMove(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            mouseMoveEvent.Invoke(context.ReadValue<Vector2>());
    }

    public void OnMouseMakeCommand(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Canceled)
            makeCommandEvent.Invoke(Mouse.current.position.ReadValue());
    }

    public void OnRotateCameraLeft(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            rotateCameraEvent.Invoke(context.ReadValue<float>());
    }

    public void OnRotateCameraRight(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            rotateCameraEvent.Invoke(context.ReadValue<float>());
    }

    public void OnResetCameraRotation(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            resetCameraRotation.Invoke();
    }

    public void OnMoveCamera(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            moveCameraEvent.Invoke(context.ReadValue<Vector2>(), true);
        else if (context.phase == InputActionPhase.Canceled)
            moveCameraEvent.Invoke(context.ReadValue<Vector2>(), false);
    }

    public void EnableGameplayInput()
    {
        _gameInput.Gameplay.Enable();
    }

    public void DisableAllInput()
    {
        _gameInput.Gameplay.Disable();
    }

    public void OnAdditionalSelect(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            additionalSelectEvent.Invoke(true);
        else if (context.phase == InputActionPhase.Canceled)
            additionalSelectEvent.Invoke(false);
    }

    public void OnStop(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            stopCommand.Invoke();
        }
    }

    public void OnLayOrStand(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            layOrStandCommand.Invoke();
        }
    }

    public void OnPlacePlatoonGhost(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            placePlatoonGhost.Invoke();
        else if (context.phase == InputActionPhase.Canceled)
            cancelPlatoonGhost.Invoke();
    }
}