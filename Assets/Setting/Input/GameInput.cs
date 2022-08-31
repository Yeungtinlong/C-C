// GENERATED AUTOMATICALLY FROM 'Assets/Setting/Input/GameInput.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @GameInput : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @GameInput()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""GameInput"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""1e3a80c7-5684-4f3f-97f2-2d77cd718ea1"",
            ""actions"": [
                {
                    ""name"": ""ZoomCamera"",
                    ""type"": ""PassThrough"",
                    ""id"": ""051c50be-b240-4297-91ce-69462b8d4b52"",
                    ""expectedControlType"": ""Axis"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""MouseControlSelect"",
                    ""type"": ""Button"",
                    ""id"": ""52b4c53d-f84a-4122-a5d1-46fc67ef8190"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""MouseMove"",
                    ""type"": ""PassThrough"",
                    ""id"": ""469a3948-218a-440a-9bc3-150bdba68d10"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""MouseMakeCommand"",
                    ""type"": ""Button"",
                    ""id"": ""a351243d-3cb0-43f8-aca0-a75c5773edf5"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""RotateCameraLeft"",
                    ""type"": ""Value"",
                    ""id"": ""125124b9-0a4b-478e-b877-09f2cfdddc46"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""RotateCameraRight"",
                    ""type"": ""Value"",
                    ""id"": ""ae1a6dd2-c849-4102-ad6f-fe1e03cb9e89"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ResetCameraRotation"",
                    ""type"": ""Button"",
                    ""id"": ""e3c9ebf1-db3a-4f4a-aa98-c41ae33691cf"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""MoveCamera"",
                    ""type"": ""Button"",
                    ""id"": ""0a360c86-12fb-45dd-a7ad-50a18ebe80e6"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""AdditionalSelect"",
                    ""type"": ""Button"",
                    ""id"": ""8858d8a8-37b2-4308-a4f9-9fbd3e6cd19a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Stop"",
                    ""type"": ""Button"",
                    ""id"": ""574c927d-1d6d-43d7-8363-ba3433c77a00"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""LayOrStand"",
                    ""type"": ""Button"",
                    ""id"": ""2ba250f3-6e08-442e-ab06-dbf46ea0b0ef"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""PlacePlatoonGhost"",
                    ""type"": ""Button"",
                    ""id"": ""6558332d-a936-4e1b-8f42-1182a7b2437b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""f1bbf6b5-c471-4f98-b5d8-83227e755254"",
                    ""path"": ""<Mouse>/scroll/y"",
                    ""interactions"": """",
                    ""processors"": ""Normalize(min=-1,max=1)"",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""ZoomCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b3f1a609-7b8e-47c5-9026-22ca81c01b76"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""MouseControlSelect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9467c3ce-5065-45be-85e9-72810ef83c1b"",
                    ""path"": ""<Mouse>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""MouseMove"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""fb14b5de-2503-4b43-b740-cf123891f2b6"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""MouseMakeCommand"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9689ac3e-9d81-4857-b7a5-5fd692900031"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": ""Scale"",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""RotateCameraLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9abf3383-9047-4ae6-9c4b-eb4209266e63"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": ""Scale(factor=-1)"",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""RotateCameraRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""449789ec-c8e6-4aab-9486-b5f850cb00b9"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""ResetCameraRotation"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""KeyBoard Arrows"",
                    ""id"": ""c07dc203-3f34-42f7-8424-38dc4f2d1fcb"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""370a2c33-9252-4a2b-bcdb-174fdae92c17"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""3f59c3e1-cc9e-47f9-b580-63ceb9950f5b"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""4a162311-8d7d-4b93-ba88-c1b2c385cdaa"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""284a1235-5e83-470b-86c2-2e48a1ebd45e"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""MoveCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""b1da920c-078e-41a3-ad2c-aef457f9d752"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""AdditionalSelect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e027bce0-1e81-46bb-8c02-786c93a0a05f"",
                    ""path"": ""<Keyboard>/p"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""Stop"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c83860bd-e99b-4b5f-be18-bca6065744fd"",
                    ""path"": ""<Keyboard>/l"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""LayOrStand"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""96e55e6e-ae5e-414e-bb6e-b5013164aaf5"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""MouseAndKeyboard"",
                    ""action"": ""PlacePlatoonGhost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""MouseAndKeyboard"",
            ""bindingGroup"": ""MouseAndKeyboard"",
            ""devices"": [
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Gameplay
        m_Gameplay = asset.FindActionMap("Gameplay", throwIfNotFound: true);
        m_Gameplay_ZoomCamera = m_Gameplay.FindAction("ZoomCamera", throwIfNotFound: true);
        m_Gameplay_MouseControlSelect = m_Gameplay.FindAction("MouseControlSelect", throwIfNotFound: true);
        m_Gameplay_MouseMove = m_Gameplay.FindAction("MouseMove", throwIfNotFound: true);
        m_Gameplay_MouseMakeCommand = m_Gameplay.FindAction("MouseMakeCommand", throwIfNotFound: true);
        m_Gameplay_RotateCameraLeft = m_Gameplay.FindAction("RotateCameraLeft", throwIfNotFound: true);
        m_Gameplay_RotateCameraRight = m_Gameplay.FindAction("RotateCameraRight", throwIfNotFound: true);
        m_Gameplay_ResetCameraRotation = m_Gameplay.FindAction("ResetCameraRotation", throwIfNotFound: true);
        m_Gameplay_MoveCamera = m_Gameplay.FindAction("MoveCamera", throwIfNotFound: true);
        m_Gameplay_AdditionalSelect = m_Gameplay.FindAction("AdditionalSelect", throwIfNotFound: true);
        m_Gameplay_Stop = m_Gameplay.FindAction("Stop", throwIfNotFound: true);
        m_Gameplay_LayOrStand = m_Gameplay.FindAction("LayOrStand", throwIfNotFound: true);
        m_Gameplay_PlacePlatoonGhost = m_Gameplay.FindAction("PlacePlatoonGhost", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Gameplay
    private readonly InputActionMap m_Gameplay;
    private IGameplayActions m_GameplayActionsCallbackInterface;
    private readonly InputAction m_Gameplay_ZoomCamera;
    private readonly InputAction m_Gameplay_MouseControlSelect;
    private readonly InputAction m_Gameplay_MouseMove;
    private readonly InputAction m_Gameplay_MouseMakeCommand;
    private readonly InputAction m_Gameplay_RotateCameraLeft;
    private readonly InputAction m_Gameplay_RotateCameraRight;
    private readonly InputAction m_Gameplay_ResetCameraRotation;
    private readonly InputAction m_Gameplay_MoveCamera;
    private readonly InputAction m_Gameplay_AdditionalSelect;
    private readonly InputAction m_Gameplay_Stop;
    private readonly InputAction m_Gameplay_LayOrStand;
    private readonly InputAction m_Gameplay_PlacePlatoonGhost;
    public struct GameplayActions
    {
        private @GameInput m_Wrapper;
        public GameplayActions(@GameInput wrapper) { m_Wrapper = wrapper; }
        public InputAction @ZoomCamera => m_Wrapper.m_Gameplay_ZoomCamera;
        public InputAction @MouseControlSelect => m_Wrapper.m_Gameplay_MouseControlSelect;
        public InputAction @MouseMove => m_Wrapper.m_Gameplay_MouseMove;
        public InputAction @MouseMakeCommand => m_Wrapper.m_Gameplay_MouseMakeCommand;
        public InputAction @RotateCameraLeft => m_Wrapper.m_Gameplay_RotateCameraLeft;
        public InputAction @RotateCameraRight => m_Wrapper.m_Gameplay_RotateCameraRight;
        public InputAction @ResetCameraRotation => m_Wrapper.m_Gameplay_ResetCameraRotation;
        public InputAction @MoveCamera => m_Wrapper.m_Gameplay_MoveCamera;
        public InputAction @AdditionalSelect => m_Wrapper.m_Gameplay_AdditionalSelect;
        public InputAction @Stop => m_Wrapper.m_Gameplay_Stop;
        public InputAction @LayOrStand => m_Wrapper.m_Gameplay_LayOrStand;
        public InputAction @PlacePlatoonGhost => m_Wrapper.m_Gameplay_PlacePlatoonGhost;
        public InputActionMap Get() { return m_Wrapper.m_Gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
        public void SetCallbacks(IGameplayActions instance)
        {
            if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
            {
                @ZoomCamera.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnZoomCamera;
                @ZoomCamera.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnZoomCamera;
                @ZoomCamera.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnZoomCamera;
                @MouseControlSelect.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseControlSelect;
                @MouseControlSelect.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseControlSelect;
                @MouseControlSelect.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseControlSelect;
                @MouseMove.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseMove;
                @MouseMove.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseMove;
                @MouseMove.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseMove;
                @MouseMakeCommand.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseMakeCommand;
                @MouseMakeCommand.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseMakeCommand;
                @MouseMakeCommand.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMouseMakeCommand;
                @RotateCameraLeft.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCameraLeft;
                @RotateCameraLeft.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCameraLeft;
                @RotateCameraLeft.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCameraLeft;
                @RotateCameraRight.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCameraRight;
                @RotateCameraRight.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCameraRight;
                @RotateCameraRight.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnRotateCameraRight;
                @ResetCameraRotation.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnResetCameraRotation;
                @ResetCameraRotation.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnResetCameraRotation;
                @ResetCameraRotation.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnResetCameraRotation;
                @MoveCamera.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMoveCamera;
                @MoveCamera.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMoveCamera;
                @MoveCamera.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMoveCamera;
                @AdditionalSelect.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAdditionalSelect;
                @AdditionalSelect.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAdditionalSelect;
                @AdditionalSelect.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAdditionalSelect;
                @Stop.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnStop;
                @Stop.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnStop;
                @Stop.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnStop;
                @LayOrStand.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLayOrStand;
                @LayOrStand.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLayOrStand;
                @LayOrStand.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLayOrStand;
                @PlacePlatoonGhost.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnPlacePlatoonGhost;
                @PlacePlatoonGhost.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnPlacePlatoonGhost;
                @PlacePlatoonGhost.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnPlacePlatoonGhost;
            }
            m_Wrapper.m_GameplayActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ZoomCamera.started += instance.OnZoomCamera;
                @ZoomCamera.performed += instance.OnZoomCamera;
                @ZoomCamera.canceled += instance.OnZoomCamera;
                @MouseControlSelect.started += instance.OnMouseControlSelect;
                @MouseControlSelect.performed += instance.OnMouseControlSelect;
                @MouseControlSelect.canceled += instance.OnMouseControlSelect;
                @MouseMove.started += instance.OnMouseMove;
                @MouseMove.performed += instance.OnMouseMove;
                @MouseMove.canceled += instance.OnMouseMove;
                @MouseMakeCommand.started += instance.OnMouseMakeCommand;
                @MouseMakeCommand.performed += instance.OnMouseMakeCommand;
                @MouseMakeCommand.canceled += instance.OnMouseMakeCommand;
                @RotateCameraLeft.started += instance.OnRotateCameraLeft;
                @RotateCameraLeft.performed += instance.OnRotateCameraLeft;
                @RotateCameraLeft.canceled += instance.OnRotateCameraLeft;
                @RotateCameraRight.started += instance.OnRotateCameraRight;
                @RotateCameraRight.performed += instance.OnRotateCameraRight;
                @RotateCameraRight.canceled += instance.OnRotateCameraRight;
                @ResetCameraRotation.started += instance.OnResetCameraRotation;
                @ResetCameraRotation.performed += instance.OnResetCameraRotation;
                @ResetCameraRotation.canceled += instance.OnResetCameraRotation;
                @MoveCamera.started += instance.OnMoveCamera;
                @MoveCamera.performed += instance.OnMoveCamera;
                @MoveCamera.canceled += instance.OnMoveCamera;
                @AdditionalSelect.started += instance.OnAdditionalSelect;
                @AdditionalSelect.performed += instance.OnAdditionalSelect;
                @AdditionalSelect.canceled += instance.OnAdditionalSelect;
                @Stop.started += instance.OnStop;
                @Stop.performed += instance.OnStop;
                @Stop.canceled += instance.OnStop;
                @LayOrStand.started += instance.OnLayOrStand;
                @LayOrStand.performed += instance.OnLayOrStand;
                @LayOrStand.canceled += instance.OnLayOrStand;
                @PlacePlatoonGhost.started += instance.OnPlacePlatoonGhost;
                @PlacePlatoonGhost.performed += instance.OnPlacePlatoonGhost;
                @PlacePlatoonGhost.canceled += instance.OnPlacePlatoonGhost;
            }
        }
    }
    public GameplayActions @Gameplay => new GameplayActions(this);
    private int m_MouseAndKeyboardSchemeIndex = -1;
    public InputControlScheme MouseAndKeyboardScheme
    {
        get
        {
            if (m_MouseAndKeyboardSchemeIndex == -1) m_MouseAndKeyboardSchemeIndex = asset.FindControlSchemeIndex("MouseAndKeyboard");
            return asset.controlSchemes[m_MouseAndKeyboardSchemeIndex];
        }
    }
    public interface IGameplayActions
    {
        void OnZoomCamera(InputAction.CallbackContext context);
        void OnMouseControlSelect(InputAction.CallbackContext context);
        void OnMouseMove(InputAction.CallbackContext context);
        void OnMouseMakeCommand(InputAction.CallbackContext context);
        void OnRotateCameraLeft(InputAction.CallbackContext context);
        void OnRotateCameraRight(InputAction.CallbackContext context);
        void OnResetCameraRotation(InputAction.CallbackContext context);
        void OnMoveCamera(InputAction.CallbackContext context);
        void OnAdditionalSelect(InputAction.CallbackContext context);
        void OnStop(InputAction.CallbackContext context);
        void OnLayOrStand(InputAction.CallbackContext context);
        void OnPlacePlatoonGhost(InputAction.CallbackContext context);
    }
}
