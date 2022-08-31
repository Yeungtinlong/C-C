using System.Collections;
using System.Collections.Generic;
using CNC.PathFinding;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelLimitSO", menuName = "Mission/Level Limit")]
public class LevelLimitSO : ScriptableObject
{
    [SerializeField] private LevelRectSO[] _levelRects = default;
    [SerializeField] private VisibilitySystemSO _visibilitySystem = default;
    [SerializeField] private BlockMapSO _blockMap = default;

    private int _currentIndex;

    public LevelRectSO CurrentLevelRect => _levelRects[_currentIndex];
    
    public void OnMissionLoaded()
    {
        _visibilitySystem.CreateMapBorder(_levelRects[_currentIndex]);
        _visibilitySystem.UpdateMapBorder(_levelRects[_currentIndex]);
        _blockMap.UpdateMapEdge(_levelRects[_currentIndex]);
    }

    public void IncreaseLevelLimit()
    {
        _currentIndex++;
        _visibilitySystem.UpdateMapBorder(_levelRects[_currentIndex]);
        _blockMap.UpdateMapEdge(_levelRects[_currentIndex]);
    }
}
