using System.Collections.Generic;
using UnityEngine;

namespace CNC.PathFinding.UnitGrid
{
    public interface IUnitGridManager
    {
        public void Initialize(int mapWidth);

        public int WorldToCellIndex(Vector2 worldPoint);

        public Vector2 CellIndexToWorld(int index);

        public void RemoveFromIndex(int index, IPathDriver driver);

        public void AddToIndex(int index, IPathDriver driver);

        public List<IPathDriver> GetUnitsInAround(Vector2 worldPoint, float radius);
        
        public void ShowGridsInDebugMode();
    }
}