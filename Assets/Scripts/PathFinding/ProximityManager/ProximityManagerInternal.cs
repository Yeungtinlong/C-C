using System.Collections.Generic;
using CNC.Utility;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace CNC.PathFinding.Proximity
{
    public class ProximityManagerInternal : IProximityManager
    {
        private const int _proximityGridCellWidth = 32;
        
        
        private int _proximityGridWidth;
        private int _proximityGridSize;
        private List<UnitGridRecord>[] _proximityGrids = new List<UnitGridRecord>[0];
        
        public void ShowBlocksInDebugMode()
        {
            for (int i = 0; i < _proximityGrids.Length; i++)
            {
                if (_proximityGrids[i].Count > 0)
                    Gizmos.color = Color.yellow;
                else
                    Gizmos.color = Color.white;

                int x = i % _proximityGridWidth;
                int z = i / _proximityGridWidth;
                Vector3 point = new Vector3(x * _proximityGridCellWidth + _proximityGridCellWidth * 0.5f, 0f,
                    z * _proximityGridCellWidth + _proximityGridCellWidth * 0.5f);
                Gizmos.DrawCube(point,
                    new Vector3(_proximityGridCellWidth * 0.95f, 0.25f, _proximityGridCellWidth * 0.95f));
            }
        }

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

        public void AddUnitToIndex(int index, IPathDriver unit)
        {
            UnitGridRecord record = new UnitGridRecord { RecordType = RecordType.IPathDriver, IPathDriver = unit };
            _proximityGrids[index].Add(record);
        }

        public void RemoveUnitByGridIndex(int index, IPathDriver unit)
        {
            _proximityGrids[index]
                .Remove(new UnitGridRecord { RecordType = RecordType.IPathDriver, IPathDriver = unit });
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

        public List<IPathDriver> GetProximityUnits(IPathDriver pathDriver, Vector3 transformPoint, float detectionRange)
        {
            Vector2 worldPoint = new Vector2(transformPoint.x, transformPoint.z);
            int minX = Mathf.Max(Mathf.FloorToInt((worldPoint.x - detectionRange) / _proximityGridCellWidth), 0);
            int minZ = Mathf.Max(Mathf.FloorToInt((worldPoint.y - detectionRange) / _proximityGridCellWidth), 0);
            int maxX = Mathf.Min(Mathf.CeilToInt((worldPoint.x + detectionRange) / _proximityGridCellWidth),
                _proximityGridWidth);
            int maxZ = Mathf.Min(Mathf.CeilToInt((worldPoint.y + detectionRange) / _proximityGridCellWidth),
                _proximityGridWidth);
            float sqrDetectionRange = Utils.Sqr(detectionRange);

            List<IPathDriver> proximityUnits = new List<IPathDriver>();

            for (int x = minX; x < maxX; x++)
            {
                for (int z = minZ; z < maxZ; z++)
                {
                    List<UnitGridRecord> unitGridRecords = _proximityGrids[x + z * _proximityGridWidth];
                    foreach (UnitGridRecord unitGridRecord in unitGridRecords)
                    {
                        IPathDriver driver = unitGridRecord.IPathDriver;
                        if (driver != null && driver != pathDriver)
                        {
                            Vector2 otherPoint = new Vector2(driver.Transform.position.x, driver.Transform.position.z);
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
            public IPathDriver IPathDriver { get; set; }
        }

        public enum RecordType
        {
            IPathDriver,
            PatrolPoint // 巡逻点，暂时无用
        }
    }
}