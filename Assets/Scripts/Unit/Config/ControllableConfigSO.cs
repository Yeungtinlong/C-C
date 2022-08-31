using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ControllableConfigSO", menuName = "Entity Config/Controllable Config")]
public class ControllableConfigSO : ScriptableObject
{
    [SerializeField] private SelectionPriority _selectionPriority = default;
    [SerializeField] private bool _isMoveable = default;
    [SerializeField] private bool _isAttackable = default;
    [SerializeField] private int _unitSizeInWorld = default;
    [SerializeField] private float _sightRange = default;

    public SelectionPriority SelectionPriority => _selectionPriority;
    public bool IsMoveable => _isMoveable;
    public bool IsAttackable => _isAttackable;
    public int UnitSizeInWolrd => _unitSizeInWorld;
    public float SightRange => _sightRange;
}

public enum SelectionPriority { OnlySingle, MultiWhenNoBattleUnit, AlwaysMulti }