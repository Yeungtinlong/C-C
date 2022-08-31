using CNC.PathFinding;
using CNC.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitProximitySO", menuName = "Path Finding/UnitProximitySO")]
public class UnitProximitySO : ScriptableObject
{
    [SerializeField] private int _proximityGridCellWidth = 32;
    private int _proximityGridWidth;
    private int _proximityGridSize;
    private List<UnitGridRecord>[] _proximityGrids = new List<UnitGridRecord>[0];

#if UNITY_EDITOR
    internal void ShowBlocksInDebugMode()
    {
        for (int i = 0; i < _proximityGrids.Length; i++)
        {
            if (_proximityGrids[i].Count > 0)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.white;

            int x = i % _proximityGridWidth;
            int z = i / _proximityGridWidth;
            Vector3 point = new Vector3(x * _proximityGridCellWidth + _proximityGridCellWidth * 0.5f, 0f, z * _proximityGridCellWidth + _proximityGridCellWidth * 0.5f);
            Gizmos.DrawCube(point, new Vector3(_proximityGridCellWidth * 0.95f, 0.25f, _proximityGridCellWidth * 0.95f));
        }
    }
#endif

    public void Initialize(int mapWidth)
    {
        _proximityGridWidth = mapWidth / _proximityGridCellWidth;
        _proximityGridSize = Utils.Sqr(_proximityGridWidth);
        _proximityGrids = new List<UnitGridRecord>[_proximityGridSize];
        for (int i = 0; i < _proximityGridSize; i++)
        {
            _proximityGrids[i] = new List<UnitGridRecord>();
        }
    }

    public void AddUnitToIndex(int index, PathDriver unit)
    {
        UnitGridRecord record = new UnitGridRecord { RecordType = RecordType.PathDriver, PathDriver = unit };
        _proximityGrids[index].Add(record);
    }

    public void RemoveUnitByGridIndex(int index, PathDriver unit)
    {
        _proximityGrids[index].Remove(new UnitGridRecord { RecordType = RecordType.PathDriver, PathDriver = unit });
    }

    public bool TryGetGridIndexFromTransformPosition(Vector3 transformPosition, out int gridIndex)
    {
        int xIndex = (int)(transformPosition.x / _proximityGridCellWidth);
        int zIndex = (int)(transformPosition.z / _proximityGridCellWidth);

        if (xIndex >= 0 && xIndex < _proximityGridWidth && zIndex >= 0 && zIndex < _proximityGridWidth)
        {
            gridIndex = xIndex + zIndex * _proximityGridWidth;
            return true;
        }

        gridIndex = -1;
        return false;
    }

    public bool IsGridIndexValid(int gridIndex)
    {
        return gridIndex >= 0 && gridIndex < _proximityGridSize;
    }

    public List<PathDriver> GetProximityUnits(PathDriver pathDriver, Vector3 transformPoint, float detectionRange)
    {
        Vector2 worldPoint = new Vector2(transformPoint.x, transformPoint.z);
        int minX = Mathf.Max(Mathf.FloorToInt((worldPoint.x - detectionRange) / _proximityGridCellWidth), 0);
        int minZ = Mathf.Max(Mathf.FloorToInt((worldPoint.y - detectionRange) / _proximityGridCellWidth), 0);
        int maxX = Mathf.Min(Mathf.CeilToInt((worldPoint.x + detectionRange) / _proximityGridCellWidth), _proximityGridWidth);
        int maxZ = Mathf.Min(Mathf.CeilToInt((worldPoint.y + detectionRange) / _proximityGridCellWidth), _proximityGridWidth);
        float sqrDetectionRange = Utils.Sqr(detectionRange);

        List<PathDriver> proximityUnits = new List<PathDriver>();

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                List<UnitGridRecord> unitGridRecords = _proximityGrids[x + z * _proximityGridWidth];
                foreach (UnitGridRecord unitGridRecord in unitGridRecords)
                {
                    PathDriver driver = unitGridRecord.PathDriver;
                    if (driver != null && driver != pathDriver)
                    {
                        Vector2 otherPoint = new Vector2(driver.transform.position.x, driver.transform.position.z);
                        float sqrDistance = (otherPoint - worldPoint).sqrMagnitude;
                        if (sqrDistance < sqrDetectionRange)
                        {
                            proximityUnits.Add(driver);
                        }
                    }
                }
            }
        }

        return proximityUnits;
    }

    public struct UnitGridRecord
    {
        public RecordType RecordType { get; set; }
        public PathDriver PathDriver { get; set; }
    }

    public enum RecordType
    {
        PathDriver,
        PatrolPoint // 巡逻点，暂时无用
    }
}
