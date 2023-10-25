using System;
using System.Collections.Generic;
using System.Linq;
using CNC.PathFinding;
using UnityEngine;
using CNC.Utility;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Commander : MonoBehaviour
{
    [SerializeField] private FactionType _faction = default;
    [SerializeField] private InputReader _inputReader = default;
    [SerializeField] private BlockMapSO _blockMapSO = default;

    [Header("Boardcasting on")] [SerializeField]
    private ChangeOutlineColorChannelSO _changeOutlineColorChannelSO = default;

    [SerializeField] private SelectionRectChannelSO _selectionRectChannelSO = default;
    [SerializeField] private SelectUnitOnScreenChannelSO _selectUnitOnScreenChannel = default;
    [SerializeField] private OrderRingChannelSO _orderRingChannel = default;
    [SerializeField] private ShowUnitMenuChannelSO _showUnitMenuChannelSO = default;

    private HashSet<Controllable> _selectedUnits = new HashSet<Controllable>();
    private HashSet<Controllable> _hoveringUnits = new HashSet<Controllable>();
    private Platoon _tempPlatoon;
    private bool _isShowingPlatoonGhost = false;
    private Vector3 _tempPlatoonCenter;

    private SelectState _selectionState;
    private Camera _mainCamera;
    private int _unitLayer;

    private int _groundLayer;

    // The threshold from single to multi selection (pixel).
    private float _singleSelectThreshold = 20f;
    private bool _isAddtionalSelecting = false;

    // The mouse point when multi selecting is begin.
    private Vector3 _startPoint;
    private Controllable _lastHoldingUnit;

    private Controllable _currentPointingUnit;
    private bool _isPointingGround;
    private Vector3 _currentPointingGroundPos;

    private bool _isPointingUI;

    public FactionType Faction => _faction;

    private void Awake()
    {
        _unitLayer = LayerMask.NameToLayer("Unit");
        _groundLayer = LayerMask.NameToLayer("Ground");
        _mainCamera = Camera.main;
        PlayerInfo.Instance.Init(_faction);
    }

    private void OnEnable()
    {
        _inputReader.mouseMoveEvent += OnMouseMove;
        _inputReader.enableSelectionEvent += OnBeginSelect;
        _inputReader.disableSelectionEvent += OnEndSelect;
        _inputReader.makeCommandEvent += OnMakeCommand;
        _inputReader.additionalSelectEvent += OnAdditionalSelectMode;
        _inputReader.stopCommand += OnStopCommand;
        _inputReader.layOrStandCommand += OnLayOrStandCommand;
        _inputReader.placePlatoonGhost += OnPlacePlatoonGhost;
        _inputReader.cancelPlatoonGhost += OnCancelPlatoonGhost;
    }

    private void OnDisable()
    {
        _inputReader.mouseMoveEvent -= OnMouseMove;
        _inputReader.enableSelectionEvent -= OnBeginSelect;
        _inputReader.disableSelectionEvent -= OnEndSelect;
        _inputReader.makeCommandEvent -= OnMakeCommand;
        _inputReader.additionalSelectEvent -= OnAdditionalSelectMode;
        _inputReader.stopCommand -= OnStopCommand;
        _inputReader.layOrStandCommand -= OnLayOrStandCommand;
        _inputReader.placePlatoonGhost -= OnPlacePlatoonGhost;
        _inputReader.cancelPlatoonGhost -= OnCancelPlatoonGhost;
    }

    private void Update()
    {
        InputScan();
        UpdatePlayerInfo();
    }

    private void InputScan()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        TryGetUnit(mousePos, out _currentPointingUnit);
        _isPointingGround = TryGetGroundPoint(mousePos, out _currentPointingGroundPos);
        _isPointingUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void UpdatePlayerInfo()
    {
        PlayerInfo.Instance.UpdateFrameCount();
    }

    private void OnMakeCommand(Vector2 mousePoint)
    {
        if (_isPointingUI)
        {
            return;
        }

        if (_currentPointingUnit != null)
        {
            FactionType targetFaction = _currentPointingUnit.Damageable.Faction;
            if (targetFaction != _faction && targetFaction != FactionType.None)
                AssignChaseCommand(_currentPointingUnit.Damageable);
        }
        else if (_isPointingGround)
        {
            _tempPlatoonCenter = _currentPointingGroundPos;
            AssignMoveCommand(_currentPointingGroundPos);
        }
    }

    private void OnBeginSelect(Vector2 startPoint)
    {
        if (_isPointingUI || _isShowingPlatoonGhost)
        {
            return;
        }

        _selectionState = SelectState.Single;
        _startPoint = startPoint;
    }

    private void OnEndSelect(Vector2 endPoint)
    {
        if (_isPointingUI && _selectionState != SelectState.Multi)
        {
            return;
        }

        switch (_selectionState)
        {
            case SelectState.Single:
                if (!_isAddtionalSelecting)
                    DeselectAll();

                if (_currentPointingUnit != null)
                {
                    if (_selectedUnits.Contains(_currentPointingUnit))
                        SingleDeselect(_currentPointingUnit);
                    else
                        SingleSelect(_currentPointingUnit, _isAddtionalSelecting);
                }

                break;

            case SelectState.Multi:
                if (!_isAddtionalSelecting)
                {
                    DeselectAll();
                    MultiSelect(_hoveringUnits.ToList(), false);
                }
                else
                {
                    foreach (Controllable hoveringUnit in _hoveringUnits)
                    {
                        if (!_selectedUnits.Contains(hoveringUnit))
                        {
                            SingleSelect(hoveringUnit, true);
                        }
                    }
                }
                
                _selectionRectChannelSO.RaiseStopDrawRectEvent();
                DePreSelectAll();

                break;
        }

        _selectionState = SelectState.None;
    }

    private void OnMouseMove(Vector2 currentPoint)
    {
        // �������м䡣
        if (_isShowingPlatoonGhost)
        {
            if (_tempPlatoon != null && _isPointingGround)
            {
                Vector3 alignment = (_currentPointingGroundPos - _tempPlatoonCenter).normalized;
                _tempPlatoon.ShowMoveCommandGhost(_tempPlatoonCenter, alignment);
            }
        }
        // û�а����м���
        else
        {
            if (_selectionState == SelectState.Single &&
                Vector3.Distance(_startPoint, currentPoint) > _singleSelectThreshold)
            {
                _selectionState = SelectState.Multi;
            }
            else if (_selectionState == SelectState.Multi)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                mousePosition.x = Mathf.Clamp(mousePosition.x, 0f, Screen.width);
                mousePosition.y = Mathf.Clamp(mousePosition.y, 0f, Screen.height);

                Rect selectionRect = Utils.CalculateRect(_startPoint, mousePosition);
                _selectionRectChannelSO.RaiseBeginDrawRectEvent(selectionRect);

                List<Controllable> includingUnits = _selectUnitOnScreenChannel.RaiseEvent(selectionRect, _faction);
                UpdateMultiPreSelecting(includingUnits);
            }
            else
            {
                if (_currentPointingUnit != null)
                {
                    bool isVisibleByView = _currentPointingUnit.IsVisibleByLocalPlayer;

                    if (isVisibleByView)
                    {
                        if (_currentPointingUnit != _lastHoldingUnit)
                        {
                            Damageable damageable = _currentPointingUnit.Damageable;
                            damageable.ToggleHealthBar(true, damageable.Faction == _faction);

                            PreSelectInternal(_currentPointingUnit);
                            _hoveringUnits.Add(_currentPointingUnit);
                            if (_lastHoldingUnit != null)
                            {
                                DePreSelectInternal(_lastHoldingUnit);
                                _hoveringUnits.Remove(_lastHoldingUnit);

                                if (!_selectedUnits.Contains(_lastHoldingUnit))
                                {
                                    damageable = _lastHoldingUnit.Damageable;
                                    damageable.ToggleHealthBar(false, damageable.Faction == _faction);
                                }
                            }

                            _lastHoldingUnit = _currentPointingUnit;
                        }
                    }
                    // ʧȥ��Ұʱ���ر�����ֵ��ʾ��
                    else if (_lastHoldingUnit == _currentPointingUnit)
                    {
                        Damageable damageable = _currentPointingUnit.Damageable;
                        damageable.ToggleHealthBar(false, damageable.Faction == _faction);
                        _lastHoldingUnit = null;
                    }
                }
                else
                {
                    if (_lastHoldingUnit != null && !_selectedUnits.Contains(_lastHoldingUnit))
                    {
                        Damageable damageable = _lastHoldingUnit.Damageable;
                        damageable.ToggleHealthBar(false, damageable.Faction == _faction);
                    }

                    DePreSelectAll();
                    _lastHoldingUnit = null;
                }
            }
        }
    }

    private void OnAdditionalSelectMode(bool isAdditionalSelecting)
    {
        _isAddtionalSelecting = isAdditionalSelecting;
    }

    public void OnPlacePlatoonGhost()
    {
        _isShowingPlatoonGhost = true;
        _tempPlatoonCenter = _currentPointingGroundPos;
    }

    public void OnCancelPlatoonGhost()
    {
        _isShowingPlatoonGhost = false;
        _tempPlatoon.ApplyPrecalculatedCommand();
        _tempPlatoon.HideGhost();
        
        _orderRingChannel.RaiseEvent(_tempPlatoonCenter, Quaternion.identity);
    }

    /// <summary>
    /// On Default "P" Key Click
    /// </summary>
    public void OnStopCommand()
    {
        AssignStopCommand();
    }

    /// <summary>
    /// On Default "L" Key Click
    /// </summary>
    public void OnLayOrStandCommand()
    {
        AssignLayCommand();

        void AssignLayCommand()
        {
            if (_tempPlatoon != null)
            {
                Command layOrStand = new Command {CommandType = CommandType.LayOrStand};
                _tempPlatoon.AssignCommand(layOrStand);
            }
        }
    }

    private void UpdateMultiPreSelecting(List<Controllable> currentPreSelectUnits)
    {
        HashSet<Controllable> removePreSelectUnits =
            new HashSet<Controllable>(_hoveringUnits.Except(currentPreSelectUnits));
        HashSet<Controllable> addPreSelectUnits =
            new HashSet<Controllable>(currentPreSelectUnits.Except(_hoveringUnits));

        foreach (Controllable unit in removePreSelectUnits)
        {
            DePreSelectInternal(unit);
            _hoveringUnits.Remove(unit);
        }

        foreach (Controllable unit in addPreSelectUnits)
        {
            PreSelectInternal(unit);
            _hoveringUnits.Add(unit);
        }
    }

    private void PreSelectInternal(Controllable controllable)
    {
        controllable.Damageable.OnDie += DePreSelectOnDie;
        controllable.PreSelect(true);

        UnitAlignment unitAlignment;

        if (controllable.Damageable.Faction == FactionType.None)
            unitAlignment = UnitAlignment.Neutral;
        else if (controllable.Damageable.Faction == _faction)
            unitAlignment = UnitAlignment.Own;
        else
            unitAlignment = UnitAlignment.Enemy;

        _changeOutlineColorChannelSO.RaiseEvent(unitAlignment);
    }

    private void DePreSelectInternal(Controllable controllable)
    {
        controllable.Damageable.OnDie -= DePreSelectOnDie;
        controllable.PreSelect(false);
    }

    private void DePreSelectAll()
    {
        if (_hoveringUnits.Count < 1)
        {
            return;
        }

        foreach (Controllable hoveringUnit in _hoveringUnits)
        {
            if (hoveringUnit != null)
            {
                DePreSelectInternal(hoveringUnit);
            }
        }

        _hoveringUnits.Clear();
    }

    private void DePreSelectOnDie(Damageable damageable)
    {
        Controllable controllable = damageable.Controllable;
        DePreSelectInternal(controllable);
        _hoveringUnits.Remove(controllable);
    }

    private void Select(Controllable unit)
    {
        _showUnitMenuChannelSO.RaiseEvent(true);
        unit.SetSelected(true);
        unit.Damageable.OnDie += DeselectOnDie;
        _selectedUnits.Add(unit);
        _lastHoldingUnit = null;
    }

    private void Deselect(Controllable unit)
    {
        _showUnitMenuChannelSO.RaiseEvent(false);
        unit.Damageable.OnDie -= DeselectOnDie;
        unit.SetSelected(false);
    }

    private void SingleSelect(Controllable unit, bool isAdditional)
    {
        if (unit.Damageable.Faction != _faction || unit.Damageable.IsDead)
            return;

        if (isAdditional && _tempPlatoon != null)
            _tempPlatoon.AddUnitIntoPlatoon(unit);
        else
            _tempPlatoon = new Platoon(unit, _blockMapSO);

        Select(unit);
    }

    private void MultiSelect(List<Controllable> units, bool isAdditional)
    {
        if (units == null || units.Count < 1)
            return;

        if (isAdditional && _tempPlatoon != null)
            _tempPlatoon.AddUnitIntoPlatoon(units);
        else
            _tempPlatoon = new Platoon(units, _blockMapSO);

        foreach (Controllable unit in units)
            if (!_selectedUnits.Contains(unit))
                Select(unit);
    }

    private void SingleDeselect(Controllable controllable)
    {
        Deselect(controllable);
        _selectedUnits.Remove(controllable);
        _tempPlatoon.RemoveUnitFromPlatoon(controllable);
        if (_tempPlatoon.IsEmptyPlatoon)
            _tempPlatoon = null;
    }

    private void DeselectAll()
    {
        if (_selectedUnits.Count < 1)
            return;

        foreach (Controllable controllable in _selectedUnits)
            Deselect(controllable);

        _selectedUnits.Clear();
        _tempPlatoon = null;
    }

    private void DeselectOnDie(Damageable damageable)
    {
        SingleDeselect(damageable.Controllable);
    }

    private bool TryGetGroundPoint(Vector2 mousePoint, out Vector3 point)
    {
        if (Physics.Raycast(_mainCamera.ScreenPointToRay(mousePoint), out RaycastHit hitGround, 1000f,
            1 << _groundLayer))
        {
            point = hitGround.point;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private bool TryGetUnit(Vector2 mousePoint, out Controllable controllable)
    {
        if (Physics.Raycast(_mainCamera.ScreenPointToRay(mousePoint), out RaycastHit hitUnit, 1000f, 1 << _unitLayer))
        {
            if (hitUnit.collider.TryGetComponent(out Controllable ctrl))
            {
                controllable = ctrl;
                return true;
            }
        }

        controllable = null;
        return false;
    }

    public List<Controllable> GetSelectedUnits()
    {
        return new List<Controllable>(_selectedUnits);
    }

    private void AssignChaseCommand(Damageable target)
    {
        if (_tempPlatoon != null)
        {
            Command chase = new Command(CommandType.Chase, target.transform.position, target);
            _tempPlatoon.AssignCommand(chase);
        }
    }

    private void AssignMoveCommand(Vector3 destination)
    {
        if (_tempPlatoon != null)
        {
            Command move = new Command(CommandType.Move, destination, null);
            _tempPlatoon.AssignCommand(move);

            _orderRingChannel.RaiseEvent(_tempPlatoonCenter, Quaternion.identity);
        }
    }

    private void AssignStopCommand()
    {
        if (_tempPlatoon != null)
        {
            Command stop = new Command {CommandType = CommandType.Stop};
            _tempPlatoon.AssignCommand(stop);
        }
    }
}

public class Command
{
    public CommandType CommandType { get; set; }
    public Vector3 Destination { get; set; }
    public Vector3 TargetAlignment { get; set; }
    public bool IsForcedAlign { get; set; }
    public float ApproachRange { get; set; }
    public float ChaseRange { get; set; }
    public Vector3 ChaseOrigin { get; set; }
    public Damageable Target { get; set; } // It will be enemy when attack or allies when cover.

    public Command()
    {
    }

    public Command(CommandType commandType, Vector3 destination, Damageable target)
    {
        CommandType = commandType;
        Destination = destination;
        Target = target;
    }
}

public enum CommandType
{
    None,
    Move,
    Chase,
    Stop,
    Cover,
    Rotate,
    LayOrStand,
    ChaseInRange,   // AI
}

public enum SelectState
{
    None,
    Single,
    Multi
}