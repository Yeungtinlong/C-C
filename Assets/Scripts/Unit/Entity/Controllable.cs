using System;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static VisibilitySystemSO;

[SelectionBase]
[RequireComponent(typeof(Damageable))]
public class Controllable : MonoBehaviour
{
    [SerializeField] private GameObject _selectionIcon = default;
    [SerializeField] private ControllableConfigSO _controllableConfigSO = default;
    [SerializeField] private VisibilitySystemSO _visibilitySystemSO = default;
    [SerializeField] private GameObject[] _outlineObjects = default;
    [SerializeField] private GameObject _modelForPlatoon = default;

    private Transform _transform;
    private Damageable _damageable;
    private Queue<Command> _commandQueue = new Queue<Command>();
    private Command _currentCommand;
    private SightPoint[] _sightPoints;
    private float _currentSightRange;
    private bool _isVisibleByLocalPlayer;
    private List<Renderer> _renderers = new List<Renderer>();
    private bool _isRendering = true;
    private int _outlineLayer;
    private int _defaultLayer;

    public Damageable Damageable => _damageable;
    public SelectionPriority SelectionPriority => _controllableConfigSO.SelectionPriority;
    public bool IsMoveable => _controllableConfigSO.IsMoveable;
    public bool IsAttackable => _controllableConfigSO.IsAttackable;
    public bool IsReceivingForcedCommand { get; set; } = false;
    public bool IsVisibleByLocalPlayer => _isVisibleByLocalPlayer;
    public int UnitSizeInWorld => _controllableConfigSO.UnitSizeInWolrd;

    private void Awake()
    {
        _transform = GetComponent<Transform>();
        _damageable = GetComponent<Damageable>();
        _renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        _outlineLayer = LayerMask.NameToLayer("Outline");
        _defaultLayer = LayerMask.NameToLayer("Default");
    }

    private void OnEnable()
    {
        _damageable.OnDie += HideGhostOnDie;
    }

    private void OnDisable()
    {
        _damageable.OnDie -= HideGhostOnDie;
    }

    private void Start()
    {
        InitSightPoints();
    }

    private void Update()
    {
        _visibilitySystemSO.UpdateUnitView(this);
        _isVisibleByLocalPlayer = _visibilitySystemSO.IsPointVisibleByLocalPlayer(_transform.position);
        UpdateUnitVisible();
    }

    private void UpdateUnitVisible()
    {
        if (_visibilitySystemSO.IsPointVisibleByLocalPlayer(_transform.position))
        {
            RenderUnit();
        }
        else
        {
            HideUnit();
        }
    }

    private void RenderUnit()
    {
        if (_isRendering)
        {
            return;
        }

        foreach (Renderer renderer in _renderers)
        {
            renderer.enabled = true;
        }

        _isRendering = true;
    }

    private void HideUnit()
    {
        if (!_isRendering)
        {
            return;
        }

        foreach (Renderer renderer in _renderers)
        {
            renderer.enabled = false;
        }

        _isRendering = false;
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            _selectionIcon.transform.localScale = Vector3.one;
            _selectionIcon.transform.DOScale(0, 1f).From().SetEase(Ease.OutBack);
        }

        if (!isSelected)
        {
            _selectionIcon.transform.DOComplete();
        }

        _selectionIcon.SetActive(isSelected);
        _damageable.ToggleHealthBar(isSelected, true);
    }

    public void PreSelect(bool isShowOutline)
    {
        foreach (GameObject outlineObject in _outlineObjects)
        {
            outlineObject.layer = isShowOutline ? _outlineLayer : _defaultLayer;
            ;
        }
    }

    public void AddCommand(Command command, bool isForced = true, bool isJumpQueue = false)
    {
        if (isForced)
        {
            if (_commandQueue.Count > 0)
                _commandQueue.Clear();

            _currentCommand = command;
            IsReceivingForcedCommand = true;
        }
        else
        {
            if (_currentCommand == null)
            {
                _currentCommand = command;
            }
            else
            {
                if (isJumpQueue)
                {
                    // 将已有Command临时存储。
                    List<Command> tempCommands = new List<Command>();
                    while (_commandQueue.Count > 0)
                    {
                        tempCommands.Add(_commandQueue.Dequeue());
                    }

                    Command tempCurrentCommand = _currentCommand;
                    // 插队Command入队。
                    _currentCommand = command;
                    // _currentCommand排在插队Command之后。
                    _commandQueue.Enqueue(tempCurrentCommand);
                    // 将原本Queue的Command入队。
                    foreach (Command tempCommand in tempCommands)
                    {
                        _commandQueue.Enqueue(tempCommand);
                    }

                    IsReceivingForcedCommand = true;
                }
                else
                {
                    _commandQueue.Enqueue(command);
                }
            }
        }
    }

    public bool TryGetCurrentCommand(out Command command)
    {
        if (_currentCommand == null && _commandQueue.Count > 0)
            _currentCommand = _commandQueue.Dequeue();

        command = _currentCommand;
        return command != null;
    }

    public void RemoveCurrentCommand()
    {
        _currentCommand = null;
        if (_commandQueue.Count > 0)
        {
            _currentCommand = _commandQueue.Dequeue();
        }
    }

    private void InitSightPoints()
    {
        _sightPoints = new SightPoint[1];
        _sightPoints[0] = new SightPoint();
        _currentSightRange = _controllableConfigSO.SightRange;
    }

    // TODO: sightPoint暂时只分配1个，为单位当前位置。
    public SightPoint[] GetSightPoints()
    {
        _sightPoints[0].Position = _transform.position + new Vector3(0f, 2f, 0f);
        return _sightPoints;
    }

    public void SetSightRange(float sightRange)
    {
        _currentSightRange = sightRange;
    }

    public float GetSightRange()
    {
        if (_sightPoints == null)
            return 0f;

        return _currentSightRange;
    }

    public void ShowModelGhostForPlatoon(Vector3 position, Vector3 alignment)
    {
        _modelForPlatoon.SetActive(true);
        _modelForPlatoon.transform.parent = null;
        _modelForPlatoon.transform.position = position;
        _modelForPlatoon.transform.forward = alignment;
    }

    public void HideModelGhostForPlatoon()
    {
        _modelForPlatoon.SetActive(false);
        _modelForPlatoon.transform.parent = _transform;
        _modelForPlatoon.transform.position = _transform.position;
        _modelForPlatoon.transform.forward = _transform.forward;
    }

    private void HideGhostOnDie(Damageable damageable)
    {
        HideModelGhostForPlatoon();
    }
}

public enum DefenceState
{
    PositiveDefence,
    StayDefence,
    
}