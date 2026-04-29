using System;
using UnityEngine;

namespace CNC.PathFinding
{
    [Flags]
    public enum BlockFlag : byte
    {
        None = 0,
        RestrictInfantry = 1,
        RestrictLightVehicle = 2,
        RestrictHeavyVehicle = 4,
        RestrictVehicle = 6,
        RestrictAll = 7,
        DeepWater = 8,
        MapEdge = 16,
        Dynamic = 32,
        Ice = 64,
        Land = 128
    }

    public interface IBlockMapManager
    {
        public int SizeX { get; }
        public int SizeZ { get; }
        public float ToWorldScale { get; }
        public Path.VisitedNode[] VisitedNodes { get; }

        public void ShowBlocksInDebugMode();

        public void Initialize(float worldSizeX, float worldSizeZ, float gridSize);

        public bool Linecast(Vector2 start, Vector2 end, int unitSizeInBlock, BlockFlag movementFlags);

        public bool IsBlockRestricted(int index, int unitSizeInBlock, BlockFlag movementFlags);
        
        public bool IsBlockRestricted(Vector2 worldPoint, int unitSizeInWorld, BlockFlag movementFlags);

        /// <summary>
        /// 利用缓存下的节点状态，判断直线内是否有阻碍。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="unitSize"></param>
        /// <param name="movementFlags"></param>
        /// <returns>当直线内没有阻碍时，返回true。</returns>
        public bool LinecastCached(Vector2 start, Vector2 end, int unitSizeInBlock, BlockFlag movementFlags);

        public bool IsBlockRestrictedCached(int index, int unitSizeInBlock, BlockFlag movementFlags);

        public Vector2 SnapToBlock(Vector2 worldPoint, float unitSizeInWorld);

        public int WorldToBlockSize(float sizeInWorld);

        public Vector2 BlockToWorldPoint(Vector2Int blockPoint, int unitSizeInBlock);

        public int BlockToIndex(Vector2Int blockPoint);

        public Vector2Int WorldToBlockPoint(Vector2 worldPoint, int unitSizeInBlock);

        public void ClearVisitedNodes();

        public void MarkBlockMap(Vector2 worldPoint, int unitSizeInWorld, BlockFlag blockFlag);

        public void UnmarkBlockMap(Vector2 worldPoint, int unitSizeInWorld, BlockFlag blockFlag);

        public void UpdateMapEdge(LevelRectSO rect);
    }
}