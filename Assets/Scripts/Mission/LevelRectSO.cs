using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRectSO", menuName = "Mission/Level Rect")]
public class LevelRectSO : ScriptableObject
{
    [SerializeField] private int _minX;
    [SerializeField] private int _maxX;
    [SerializeField] private int _minZ;
    [SerializeField] private int _maxZ;

    public int MinX => _minX;
    public int MaxX => _maxX;
    public int MinZ => _minZ;
    public int MaxZ => _maxZ;

    public Vector3 Center => new Vector3(0.5f * (_minX + _maxX), 0f, 0.5f * (_minZ + _maxZ));
    public Vector3 Size => new Vector3(_maxX - _minX, 0f, _maxZ - _minZ);
}