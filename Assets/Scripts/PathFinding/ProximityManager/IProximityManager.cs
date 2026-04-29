using System.Collections.Generic;
using UnityEngine;

namespace CNC.PathFinding.Proximity
{
    public struct UnitGridRecord
    {
        public RecordType RecordType { get; set; }
        public IPathDriver PathDriver { get; set; }
    }

    public enum RecordType
    {
        PathDriver,
        PatrolPoint // 巡逻点，暂时无用
    }
    
    public interface IProximityManager
    {
        public void Initialize(int mapWidth);

        public void AddUnitToIndex(int index, IPathDriver unit);

        public void RemoveUnitByGridIndex(int index, IPathDriver unit);

        public bool TryGetGridIndexFromTransformPosition(Vector3 transformPosition, out int gridIndex);

        public bool IsGridIndexValid(int gridIndex);

        public List<IPathDriver> GetProximityUnits(IPathDriver pathDriver, Vector3 transformPoint,
            float detectionRange);

        public void ShowBlocksInDebugMode();
    }
}