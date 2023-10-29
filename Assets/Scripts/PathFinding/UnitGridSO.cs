using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CNC.Utility;

namespace CNC.PathFinding
{
    [CreateAssetMenu(fileName = "UnitGridSO", menuName = "Path Finding/Unit Grid")]
    public class UnitGridSO : ScriptableObject
    {
        private List<IPathDriver>[] _unitGrid;
        private int _unitGridCellWidth = 16;
        private int _unitGridWidth;
        private int _unitGridSize;

        public void Initialize(int mapWidth)
        {
            _unitGridWidth = mapWidth / _unitGridCellWidth;
            _unitGridSize = _unitGridWidth * _unitGridWidth;
            _unitGrid = new List<IPathDriver>[_unitGridSize];
            for (int i = 0; i < _unitGridSize; i++)
            {
                _unitGrid[i] = new List<IPathDriver>();
            }
        }

        public int WorldToCellIndex(Vector2 worldPoint)
        {
            int x = Mathf.FloorToInt(worldPoint.x / _unitGridCellWidth);
            int z = Mathf.FloorToInt(worldPoint.y / _unitGridCellWidth);
            if (x >= 0 && x < _unitGridWidth && z >= 0 && z < _unitGridWidth)
            {
                return x + z * _unitGridWidth;
            }
            return -1;
        }

        public Vector2 CellIndexToWorld(int index)
        {
            float x = (index % _unitGridWidth) * _unitGridCellWidth + 0.5f * _unitGridCellWidth;
            float z = (int)(index / _unitGridWidth) * _unitGridCellWidth + 0.5f * _unitGridCellWidth;
            return new Vector2(x, z);
        }

        public void RemoveFromIndex(int index, IPathDriver driver)
        {
            _unitGrid[index].Remove(driver);
        }

        public void AddToIndex(int index, IPathDriver driver)
        {
            _unitGrid[index].Add(driver);
        }

        public List<IPathDriver> GetUnitsInAround(Vector2 worldPoint, float radius)
        {
            List<IPathDriver> unitsInAround = new List<IPathDriver>();

            int minX = Mathf.Max(Mathf.FloorToInt((worldPoint.x - radius) / _unitGridCellWidth), 0);
            int maxX = Mathf.Min(Mathf.CeilToInt((worldPoint.x + radius) / _unitGridCellWidth), _unitGridWidth - 1);
            int minZ = Mathf.Max(Mathf.FloorToInt((worldPoint.y - radius) / _unitGridCellWidth), 0);
            int maxZ = Mathf.Min(Mathf.CeilToInt((worldPoint.y + radius) / _unitGridCellWidth), _unitGridWidth - 1);

            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minZ; j < maxZ; j++)
                {
                    foreach (IPathDriver driver in _unitGrid[i + j * _unitGridWidth])
                    {
                        if (Utils.SqrDistance(PathDriverUtils.TransformToWorldPoint(driver.Transform.position), worldPoint) < Utils.Sqr(radius))
                        {
                            unitsInAround.Add(driver);
                        }
                    }
                }
            }

            return unitsInAround;
        }

#if UNITY_EDITOR
        public void ShowGridsInDebugMode()
        {
            if (!Application.isPlaying)
                return;

            for (int i = 0; i < _unitGrid.Length; i++)
            {
                if (_unitGrid[i].Count > 0)
                {
                    Vector2 world = CellIndexToWorld(i);
                    Vector3 transform = new Vector3(world.x, 0f, world.y);
                    Gizmos.DrawCube(transform, new Vector3(_unitGridCellWidth * 0.95f, 0.3f, _unitGridCellWidth * 0.95f));
                }
            }
        }
    }
#endif
}

