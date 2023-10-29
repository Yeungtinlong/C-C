using System;
using System.Collections.Generic;
using UnityEngine;
using VisitedNode = CNC.PathFinding.Path.VisitedNode;
using CNC.Utility;
using UnityEditor;
using static CNC.PathFinding.Path;

namespace CNC.PathFinding
{
    [CreateAssetMenu(fileName = "BlockMapSO", menuName = "Path Finding/BlockMapSO")]
    public class BlockMapSO : ScriptableObject
    {
        private int _sizeX;
        private int _sizeZ;
        private float _toWorldScale; // gridSize
        private int _mapSize;
        private BlockFlag[] _blockFlagMap;
        private VisitedNode[] _visitedNodes;

        internal int SizeX => _sizeX;
        internal int SizeZ => _sizeZ;
        internal float ToWorldScale => _toWorldScale;
        internal VisitedNode[] VisitedNodes => _visitedNodes;

#if UNITY_EDITOR
        private bool _isInitializedDebugger = false;
        private List<BlockMapDebugUI> _debugUIs = new List<BlockMapDebugUI>();

        internal void ShowBlocksInDebugMode()
        {
            for (int i = 0; i < _blockFlagMap.Length; i++)
            {
                int x = i % _sizeX;
                int z = i / _sizeX;
                Vector3 transformPos = new Vector3(x * _toWorldScale + 0.5f * _toWorldScale, 0.01f,
                    z * _toWorldScale + 0.5f * _toWorldScale);
                Gizmos.color = _blockFlagMap[i].HasFlag(BlockFlag.Dynamic) ? Color.red : Color.green;
                Gizmos.DrawCube(transformPos, new Vector3(0.9f * _toWorldScale, 0.01f, 0.9f * _toWorldScale));
            }
        }
#endif
        
        // 参数sizeX和sizeZ是世界地图的规格，gridSize是一个blockMap格子占用世界空间的大小，当blockMap倍数于世界格子时，gridSize就小于1。
        internal void Initialize(float worldSizeX, float worldSizeZ, float gridSize)
        {
            _toWorldScale = gridSize;
            _sizeX = Mathf.RoundToInt(worldSizeX / gridSize);
            _sizeZ = Mathf.RoundToInt(worldSizeZ / gridSize);
            _mapSize = _sizeX * _sizeZ;
            _blockFlagMap = new BlockFlag[_mapSize];
            _visitedNodes = (_mapSize != PathNodeManager.MAP_SIZE)
                ? new VisitedNode[_mapSize]
                : PathNodeManager.VisitedNodes;
        }

        internal bool Linecast(Vector2 start, Vector2 end, int unitSizeInBlock, BlockFlag movementFlags)
        {
            start.x = start.x / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;
            start.y = start.y / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;
            end.x = end.x / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;
            end.y = end.y / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;

            if (start.x < 0f || start.x + unitSizeInBlock > _sizeX || start.y < 0f ||
                start.y + unitSizeInBlock > _sizeZ)
                return false;
            if (end.x < 0f || end.x + unitSizeInBlock > _sizeX || end.y < 0f || end.y + unitSizeInBlock > _sizeZ)
                return false;

            float deltaX = Mathf.Abs(start.x - end.x);
            float deltaZ = Mathf.Abs(start.y - end.y);

            int startX = Mathf.FloorToInt(start.x);
            int startZ = Mathf.FloorToInt(start.y);
            int endX = Mathf.FloorToInt(end.x);
            int endZ = Mathf.FloorToInt(end.y);

            if (startX == endX && startZ == endZ)
                return !IsBlockRestricted(startX + startZ * _sizeX, unitSizeInBlock, movementFlags);

            // 纵向检测
            if (deltaZ >= deltaX)
            {
                if (start.y > end.y)
                {
                    Utils.Swap(ref start, ref end);
                    Utils.Swap(ref startZ, ref endZ);
                }

                float k = (end.x - start.x) / (end.y - start.y);
                float b = start.x - k * start.y;

                for (int i = startZ; i <= endZ; i++)
                {
                    int floorX, ceilX;
                    if (k >= 0f)
                    {
                        floorX = Mathf.FloorToInt(k * Mathf.Max(start.y, i) + b);
                        ceilX = Mathf.CeilToInt(k * Mathf.Min(end.y, i + 1) + b) - 1;
                    }
                    else
                    {
                        floorX = Mathf.CeilToInt(k * Mathf.Max(start.y, i) + b) - 1;
                        ceilX = Mathf.FloorToInt(k * Mathf.Min(end.y, i + 1) + b);
                    }

                    if (IsBlockRestricted(floorX + i * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                    if (floorX != ceilX && IsBlockRestricted(ceilX + i * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                }

                return true;
            }
            // deltaZ < deltaX，横向检测。
            else
            {
                if (start.x > end.x)
                {
                    Utils.Swap(ref start, ref end);
                    Utils.Swap(ref startX, ref endX);
                }

                float k = (end.y - start.y) / (end.x - start.x);
                float b = start.y - k * start.x;

                for (int i = startX; i <= endX; i++)
                {
                    int floorZ, ceilZ;
                    if (k >= 0f)
                    {
                        floorZ = Mathf.FloorToInt(k * Mathf.Max(start.x, i) + b);
                        ceilZ = Mathf.CeilToInt(k * Mathf.Min(end.x, i + 1) + b) - 1;
                    }
                    else
                    {
                        floorZ = Mathf.CeilToInt(k * Mathf.Max(start.x, i) + b) - 1;
                        ceilZ = Mathf.FloorToInt(k * Mathf.Min(end.x, i + 1) + b);
                    }

                    if (IsBlockRestricted(i + floorZ * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                    if (floorZ != ceilZ && IsBlockRestricted(i + ceilZ * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 检查该世界坐标地点是否阻碍某种类型。
        /// </summary>
        /// <param name="worldPoint"></param>
        /// <param name="unitSizeInWorld"></param>
        /// <param name="movementFlags"></param>
        /// <returns></returns>
        internal bool IsBlockRestricted(Vector2 worldPoint, int unitSizeInWorld, BlockFlag movementFlags)
        {
            int unitSizeInBlock = WorldToBlockSize(unitSizeInWorld);
            Vector2Int blockPoint = WorldToBlockPoint(worldPoint, unitSizeInBlock);
            int index = BlockToIndex(blockPoint);
            return IsBlockRestricted(index, unitSizeInBlock, movementFlags);
        }

        internal bool IsBlockRestricted(int index, int unitSizeInBlock, BlockFlag movementFlags)
        {
            if (unitSizeInBlock == 1)
                return (_blockFlagMap[index] & movementFlags) != 0;
            if (unitSizeInBlock == 2)
                return (_blockFlagMap[index] & movementFlags) != 0 || (_blockFlagMap[index + 1] & movementFlags) != 0 ||
                       (_blockFlagMap[index + _sizeX] & movementFlags) != 0 ||
                       (_blockFlagMap[index + _sizeX + 1] & movementFlags) != 0;
            if (unitSizeInBlock == 4)
            {
                if (LineCheck02(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck04(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck04(index, movementFlags))
                    return true;
                index += _sizeX;
                return LineCheck02(index + 1, movementFlags);
            }
            else if (unitSizeInBlock == 5)
            {
                if (LineCheck03(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck05(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck05(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck05(index, movementFlags))
                    return true;
                index += _sizeX;
                return LineCheck03(index + 1, movementFlags);
            }
            else if (unitSizeInBlock == 6)
            {
                if (LineCheck04(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck06(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck06(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck06(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck06(index, movementFlags))
                    return true;
                index += _sizeX;
                return LineCheck04(index + 1, movementFlags);
            }
            else if (unitSizeInBlock == 7)
            {
                if (LineCheck03(index + 2, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck05(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck07(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck07(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck07(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck05(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                return LineCheck03(index + 2, movementFlags);
            }
            else if (unitSizeInBlock == 8)
            {
                if (LineCheck04(index + 2, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck06(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck06(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                return LineCheck04(index + 2, movementFlags);
            }
            else if (unitSizeInBlock == 9)
            {
                if (LineCheck05(index + 2, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck07(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck09(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck09(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck09(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck09(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck09(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck07(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                return LineCheck05(index + 2, movementFlags);
            }
            else
            {
                if (unitSizeInBlock != 10)
                    Debug.LogWarning("IsBlockRestricted: Size is invalid.");

                if (LineCheck04(index + 3, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck10(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck10(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck10(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck10(index, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                if (LineCheck08(index + 1, movementFlags))
                    return true;
                index += _sizeX;
                return LineCheck04(index + 3, movementFlags);
            }
        }

        private bool LineCheck02(int index, BlockFlag flag)
        {
            return (_blockFlagMap[index] & flag) != 0 || (_blockFlagMap[index + 1] & flag) != 0;
        }

        private bool LineCheck03(int index, BlockFlag flag)
        {
            return (_blockFlagMap[index] & flag) != 0 || (_blockFlagMap[index + 1] & flag) != 0 ||
                   (_blockFlagMap[index + 2] & flag) != 0;
        }

        private bool LineCheck04(int index, BlockFlag flag)
        {
            return LineCheck02(index, flag) || LineCheck02(index + 2, flag);
        }

        private bool LineCheck05(int index, BlockFlag flag)
        {
            return LineCheck02(index, flag) || LineCheck03(index + 2, flag);
        }

        private bool LineCheck06(int index, BlockFlag flag)
        {
            return LineCheck03(index, flag) || LineCheck03(index + 3, flag);
        }

        private bool LineCheck07(int index, BlockFlag flag)
        {
            return LineCheck03(index, flag) || LineCheck04(index + 3, flag);
        }

        private bool LineCheck08(int index, BlockFlag flag)
        {
            return LineCheck04(index, flag) || LineCheck04(index + 4, flag);
        }

        private bool LineCheck09(int index, BlockFlag flag)
        {
            return LineCheck04(index, flag) || LineCheck05(index + 5, flag);
        }

        private bool LineCheck10(int index, BlockFlag flag)
        {
            return LineCheck05(index, flag) || LineCheck05(index + 5, flag);
        }

        /// <summary>
        /// 利用缓存下的节点状态，判断直线内是否有阻碍。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="unitSize"></param>
        /// <param name="movementFlags"></param>
        /// <returns>当直线内没有阻碍时，返回true。</returns>
        internal bool LinecastCached(Vector2 start, Vector2 end, int unitSizeInBlock, BlockFlag movementFlags)
        {
            start.x = start.x / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;
            start.y = start.y / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;
            end.x = end.x / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;
            end.y = end.y / _toWorldScale - 0.5f * unitSizeInBlock + 0.5f;

            if (start.x < 0f || start.x + unitSizeInBlock > _sizeX || start.y < 0f ||
                start.y + unitSizeInBlock > _sizeZ)
                return false;
            if (end.x < 0f || end.x + unitSizeInBlock > _sizeX || end.y < 0f || end.y + unitSizeInBlock > _sizeZ)
                return false;

            float deltaX = Mathf.Abs(start.x - end.x);
            float deltaZ = Mathf.Abs(start.y - end.y);

            int startX = Mathf.FloorToInt(start.x);
            int startZ = Mathf.FloorToInt(start.y);
            int endX = Mathf.FloorToInt(end.x);
            int endZ = Mathf.FloorToInt(end.y);

            if (startX == endX && startZ == endZ)
                return !IsBlockRestrictedCached(startX + startZ * _sizeX, unitSizeInBlock, movementFlags);

            if (deltaZ >= deltaX)
            {
                if (start.y > end.y)
                {
                    Utils.Swap(ref start, ref end);
                    Utils.Swap(ref startZ, ref endZ);
                }

                float k = (end.x - start.x) / (end.y - start.y);
                float b = start.x - k * start.y;

                for (int i = startZ; i <= endZ; i++)
                {
                    int floorX, ceilX;
                    if (k >= 0f)
                    {
                        floorX = Mathf.FloorToInt(k * Mathf.Max(start.y, i) + b);
                        ceilX = Mathf.CeilToInt(k * Mathf.Min(end.y, i + 1) + b) - 1;
                    }
                    else
                    {
                        floorX = Mathf.CeilToInt(k * Mathf.Max(start.y, i) + b) - 1;
                        ceilX = Mathf.FloorToInt(k * Mathf.Min(end.y, i + 1) + b);
                    }

                    if (IsBlockRestrictedCached(floorX + i * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                    if (floorX != ceilX && IsBlockRestrictedCached(ceilX + i * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                }

                return true;
            }
            // deltaZ < deltaX
            else
            {
                if (start.x > end.x)
                {
                    Utils.Swap(ref start, ref end);
                    Utils.Swap(ref startX, ref endX);
                }

                float k = (end.y - start.y) / (end.x - start.x);
                float b = start.y - k * start.x;

                for (int i = startX; i <= endX; i++)
                {
                    int floorZ, ceilZ;
                    if (k >= 0f)
                    {
                        floorZ = Mathf.FloorToInt(k * Mathf.Max(start.x, i) + b);
                        ceilZ = Mathf.CeilToInt(k * Mathf.Min(end.x, i + 1) + b) - 1;
                    }
                    else
                    {
                        floorZ = Mathf.CeilToInt(k * Mathf.Max(start.x, i) + b) - 1;
                        ceilZ = Mathf.FloorToInt(k * Mathf.Min(end.x, i + 1) + b);
                    }

                    if (IsBlockRestrictedCached(i + floorZ * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                    if (floorZ != ceilZ && IsBlockRestrictedCached(i + ceilZ * _sizeX, unitSizeInBlock, movementFlags))
                        return false;
                }

                return true;
            }
        }

        internal bool IsBlockRestrictedCached(int index, int unitSizeInBlock, BlockFlag movementFlags)
        {
            NodeState state = _visitedNodes[index].NodeState;
            if (state == NodeState.Blocked)
                return true;
            if (state == NodeState.Walkable)
                return false;
            if (IsBlockRestricted(index, unitSizeInBlock, movementFlags))
            {
                _visitedNodes[index].NodeState = NodeState.Blocked;
                return true;
            }
            else
            {
                _visitedNodes[index].NodeState = NodeState.Walkable;
                return false;
            }
        }

        internal Vector2 SnapToBlock(Vector2 worldPoint, float unitSizeInWorld)
        {
            int unitSizeInBlock = WorldToBlockSize(unitSizeInWorld);
            Vector2Int blockPoint = WorldToBlockPoint(worldPoint, unitSizeInBlock);
            return BlockToWorldPoint(blockPoint, unitSizeInBlock);
        }

        internal int WorldToBlockSize(float sizeInWorld)
        {
            return Mathf.RoundToInt(sizeInWorld / _toWorldScale);
        }

        internal Vector2 BlockToWorldPoint(Vector2Int blockPoint, int unitSizeInBlock)
        {
            return new Vector2((blockPoint.x + 0.5f * unitSizeInBlock) * _toWorldScale,
                (blockPoint.y + 0.5f * unitSizeInBlock) * _toWorldScale);
        }

        internal int BlockToIndex(Vector2Int blockPoint)
        {
            return blockPoint.x + _sizeX * blockPoint.y;
        }

        internal Vector2Int WorldToBlockPoint(Vector2 worldPoint, int unitSizeInBlock)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(worldPoint.x / _toWorldScale - 0.5f * unitSizeInBlock), 0,
                _sizeX - unitSizeInBlock);
            int z = Mathf.Clamp(Mathf.RoundToInt(worldPoint.y / _toWorldScale - 0.5f * unitSizeInBlock), 0,
                _sizeZ - unitSizeInBlock);
            return new Vector2Int(x, z);
        }

        internal void ClearVisitedNodes()
        {
            Array.Clear(_visitedNodes, 0, _mapSize);
        }

        internal void MarkBlockMap(Vector2 worldPoint, int unitSizeInWorld, BlockFlag blockFlag)
        {
            int unitSizeInBlock = WorldToBlockSize(unitSizeInWorld);
            Vector2Int blockPoint = WorldToBlockPoint(worldPoint, unitSizeInBlock);
            int index = BlockToIndex(blockPoint);
            MarkBlockMap(index, unitSizeInBlock, blockFlag);
        }

        private void MarkBlockMap(int index, int unitSizeInBlock, BlockFlag blockFlag)
        {
            float radius = unitSizeInBlock * 0.5f;
            float sqrRadius = Utils.Sqr(radius);
            for (int i = 0; i < unitSizeInBlock; i++)
            for (int j = 0; j < unitSizeInBlock; j++)
                if (Utils.Sqr(i + 0.5f - radius) + Utils.Sqr(j + 0.5f - radius) < sqrRadius)
                    _blockFlagMap[index + i + _sizeX * j] |= blockFlag;
        }

        internal void UnmarkBlockMap(Vector2 worldPoint, int unitSizeInWorld, BlockFlag blockFlag)
        {
            int unitSizeInBlock = WorldToBlockSize(unitSizeInWorld);
            Vector2Int blockPoint = WorldToBlockPoint(worldPoint, unitSizeInBlock);
            int index = BlockToIndex(blockPoint);
            UnmarkBlockMap(index, unitSizeInBlock, blockFlag);
        }

        private void UnmarkBlockMap(int index, int unitSizeInBlock, BlockFlag blockFlag)
        {
            float radius = unitSizeInBlock * 0.5f;
            float sqrRadius = Utils.Sqr(radius);
            for (int i = 0; i < unitSizeInBlock; i++)
            for (int j = 0; j < unitSizeInBlock; j++)
                if (Utils.Sqr(i + 0.5f - radius) + Utils.Sqr(j + 0.5f - radius) < sqrRadius)
                    _blockFlagMap[index + i + _sizeX * j] &= ~blockFlag;
        }

        public void UpdateMapEdge(LevelRectSO rect)
        {
            int minX = Mathf.Clamp((int)(rect.MinX / _toWorldScale + 0.5f), 0, _sizeX);
            int minZ = Mathf.Clamp((int)(rect.MinZ / _toWorldScale + 0.5f), 0, _sizeX);
            int maxX = Mathf.Clamp((int)(rect.MaxX / _toWorldScale + 0.5f), 0, _sizeX);
            int maxZ = Mathf.Clamp((int)(rect.MaxZ / _toWorldScale + 0.5f), 0, _sizeX);

            for (int i = 0; i < _sizeZ; i++)
            {
                for (int j = 0; j < _sizeX; j++)
                {
                    if (j < minX || j >= maxX || i < minZ || i >= maxZ)
                    {
                        _blockFlagMap[j + i * _sizeX] |= BlockFlag.MapEdge;
                    }
                    else
                    {
                        _blockFlagMap[j + i * _sizeX] &= ~BlockFlag.MapEdge;
                    }
                }
            }
        }

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
    }
}