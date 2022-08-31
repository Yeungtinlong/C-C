using CNC.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using static CNC.PathFinding.BlockMapSO;

namespace CNC.PathFinding
{
    internal class Path
    {
        private int _unitSizeInBlock;
        private BlockMapSO _blockMap;
        private PathResult _result;
        private BlockFlag _movementMask;
        private List<Vector2> _vectorPath = new List<Vector2>();
        private Vector2Int _originalStartPos;
        private Vector2Int _startPos;
        private Vector2Int _endPos;
        private Heap<PathNode> _openList;

        private const int HEURISTIC_SCALE = 3;
        private const float PROJECT_STEP = 0.5f;

        public List<Vector2> VectorPath => _vectorPath;
        public PathResult Result => _result;

        internal Path(BlockMapSO blockMap)
        {
            _blockMap = blockMap;
        }
        
        public static BlockFlag GetMovementFlags(UnitType unitType)
        {
            BlockFlag blockFlag = BlockFlag.DeepWater | BlockFlag.MapEdge | BlockFlag.Dynamic;

            if (unitType == UnitType.Infantry)
                blockFlag |= BlockFlag.RestrictInfantry;
            else if (unitType == UnitType.Vehicle)
                blockFlag |= BlockFlag.RestrictLightVehicle;
            else if (unitType == UnitType.Armor)
                blockFlag |= BlockFlag.RestrictHeavyVehicle;

            return blockFlag;
        }

        internal void FindPath(Vector2 start, Vector2 end, float unitSizeInWorld, float maxRange, BlockFlag movementMask)
        {
            _result = PathResult.Success;
            _unitSizeInBlock = Mathf.Max(1, Mathf.RoundToInt(unitSizeInWorld / _blockMap.ToWorldScale));
            _movementMask = movementMask;
            int maxSteps = maxRange < 0f ? -1 : Mathf.CeilToInt(maxRange / _blockMap.ToWorldScale);

            if (_blockMap.Linecast(start, end, _unitSizeInBlock, movementMask))
            {
                _vectorPath.Add(start);
                _vectorPath.Add(end);
                return;
            }

            // 单位左下角坐标
            _originalStartPos = _blockMap.WorldToBlockPoint(start, _unitSizeInBlock);
            _startPos = _originalStartPos;

            // 如果起点受阻，寻找最接近的合法起点
            if (_blockMap.IsBlockRestricted(GetIndex(_startPos), _unitSizeInBlock, movementMask))
            {
                _startPos = GetNearestValidPoint(_startPos);
            }

            _endPos = _blockMap.WorldToBlockPoint(end, _unitSizeInBlock);

            if ((_blockMap.BlockToWorldPoint(_endPos, _unitSizeInBlock) - end).magnitude > _blockMap.ToWorldScale)
            {
                end = _blockMap.BlockToWorldPoint(_endPos, _unitSizeInBlock);
            }

            // 如果终点受阻，寻找最接近的合法终点
            if (_blockMap.IsBlockRestricted(GetIndex(_endPos), _unitSizeInBlock, movementMask))
            {
                _endPos = GetNearestValidPoint(_endPos);
                end = _blockMap.BlockToWorldPoint(_endPos, _unitSizeInBlock);
                if (GetIndex(_endPos) == GetIndex(_originalStartPos))
                {
                    _vectorPath.Add(start);
                    _vectorPath.Add(end);
                    _result = PathResult.FailStart;
                    return;
                }
            }

            if (_blockMap.IsBlockRestricted(GetIndex(_startPos), _unitSizeInBlock, movementMask) || _blockMap.IsBlockRestricted(GetIndex(_endPos), _unitSizeInBlock, movementMask))
            {
                Debug.LogError($"Path: Can't find the path from {start} to {end}.");
                if (GetIndex(_endPos) == GetIndex(_originalStartPos))
                {
                    _vectorPath.Add(start);
                    _vectorPath.Add(end);
                    _result = PathResult.FailStart;
                    return;
                }
            }

            GenerateAStarPath(start, end, maxSteps);
        }

        private void GenerateAStarPath(Vector2 start, Vector2 end, int maxSteps)
        {
            _blockMap.ClearVisitedNodes();
            _openList = PathNodeManager.OpenList;

            if (!_openList.IsEmpty)
            {
                Debug.LogError("GenerateAStarPath: OpenList is not empty.");
                _openList.Clear();
            }

            Vector2Int tempPos = _startPos;
            float h = AddOpenNode(_startPos, 0f, 0, Direction.None);

            while (!_openList.IsEmpty)
            {
                PathNode pathNode = _openList.Pop();
                if (GetIndex(pathNode.Position) == GetIndex(_endPos))
                {
                    // 整理节点生成路径
                    CollectPath(pathNode.Position, start, end);
                    // 将所有使用过的节点归还对象池
                    ReleaseAllOpenNodes();
                    return;
                }

                // 如果距离终点的损耗 H 比之前更小，则更新临时 h 以及临时位置。
                if (pathNode.H < h)
                {
                    h = pathNode.H;
                    tempPos = pathNode.Position;
                }

                if (maxSteps > 0 && pathNode.Depth + ManhattanDistance(pathNode.Position, _endPos) >= maxSteps)
                    CloseOpenNode(pathNode);
                else
                    OpenNeighbours(pathNode);
            }

            CollectPath(tempPos, start, _blockMap.BlockToWorldPoint(tempPos, _unitSizeInBlock));
            _result = PathResult.Fail;
        }

        private static int ManhattanDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        private void OpenNeighbours(PathNode pathNode)
        {
            CheckNeighbour(pathNode, 0, -1, 1f, Direction.Down);
            CheckNeighbour(pathNode, 1, 0, 1f, Direction.Right);
            CheckNeighbour(pathNode, 0, 1, 1f, Direction.Up);
            CheckNeighbour(pathNode, -1, 0, 1f, Direction.Left);
            CheckNeighbour(pathNode, 1, -1, 1.4142f, Direction.DownRight);
            CheckNeighbour(pathNode, 1, 1, 1.4142f, Direction.UpRight);
            CheckNeighbour(pathNode, -1, 1, 1.4142f, Direction.UpLeft);
            CheckNeighbour(pathNode, -1, -1, 1.4142f, Direction.DownLeft);
            CloseOpenNode(pathNode);
        }

        private void CheckNeighbour(PathNode pathNode, int xStep, int zStep, float cost, Direction direction)
        {
            Vector2Int blockPoint = pathNode.Position + new Vector2Int(xStep, zStep);

            if (!IsBlockMapPointValid(blockPoint))
                return;

            int nodeIndex = GetIndex(blockPoint);
            NodeState nodeState = _blockMap.VisitedNodes[nodeIndex].NodeState;

            if (nodeState == NodeState.Blocked)
                return;

            if (nodeState == NodeState.Unvisited)
            {
                if (_blockMap.IsBlockRestricted(nodeIndex, _unitSizeInBlock, _movementMask))
                {
                    _blockMap.VisitedNodes[nodeIndex].NodeState = NodeState.Blocked;
                    return;
                }

                AddOpenNode(blockPoint, pathNode.G + cost, pathNode.Depth + 1, direction);
            }
            // nodeState == NodeState.Walkable
            else
            {
                PathNode pathNodeByIndex = PathNodeManager.GetPathNodeByIndex(_blockMap.VisitedNodes[nodeIndex].OpenNodeIndex);
                if (pathNodeByIndex != null && pathNodeByIndex.G > pathNode.G + cost)
                {
                    pathNodeByIndex.G = pathNode.G + cost;
                    pathNodeByIndex.Depth = pathNode.Depth + 1;
                    _openList.UpdateItem(pathNodeByIndex);
                    _blockMap.VisitedNodes[nodeIndex].Direction = direction;
                }
            }
        }

        private void CloseOpenNode(PathNode pathNode)
        {
            _blockMap.VisitedNodes[GetIndex(pathNode.Position)].OpenNodeIndex = 0;
            PathNodeManager.ReleasePathNode(pathNode);
        }

        private void ReleaseAllOpenNodes()
        {
            PathNode[] heap = _openList.Items;
            int count = _openList.Count;

            for (int i = 0; i < count; i++)
                PathNodeManager.ReleasePathNode(heap[i]);

            _openList.Clear();
        }

        private void CollectPath(Vector2Int tail, Vector2 start, Vector2 end)
        {
            List<Vector2> reversePath = new List<Vector2>();
            reversePath.Add(end);

            while (true)
            {
                reversePath.Add(_blockMap.BlockToWorldPoint(tail, _unitSizeInBlock));
                Direction direction = _blockMap.VisitedNodes[GetIndex(tail)].Direction;
                if (direction == Direction.None)
                    break;

                switch (direction)
                {
                    case Direction.Left:
                        tail.x++;
                        break;
                    case Direction.Right:
                        tail.x--;
                        break;
                    case Direction.Up:
                        tail.y--;
                        break;
                    case Direction.Down:
                        tail.y++;
                        break;
                    case Direction.UpLeft:
                        tail.x++;
                        tail.y--;
                        break;
                    case Direction.UpRight:
                        tail.x--;
                        tail.y--;
                        break;
                    case Direction.DownLeft:
                        tail.x++;
                        tail.y++;
                        break;
                    case Direction.DownRight:
                        tail.x--;
                        tail.y++;
                        break;
                }
            }

            reversePath.Add(start);
            int index = reversePath.Count - 1;
            Vector2 point = reversePath[index];
            _vectorPath.Add(point);

            //// Test: 测试节点是否合理
            //for (int i = reversePath.Count - 2; i >= 0; i--)
            //{
            //    _vectorPath.Add(reversePath[i]);
            //}

            // 若干个节点之间假如没有障碍，则精简中途节点
            do
            {
                int i;
                // 当节点数大于等于64个时，
                for (i = 0; i < index - 1; i += 1)
                {
                    // 如果两点一线内没有阻碍，则将此点作为基点，继续检测。
                    if (_blockMap.LinecastCached(point, reversePath[i], _unitSizeInBlock, _movementMask))
                    {
                        break;
                    }
                }
                index = i;
                point = reversePath[index];
                _vectorPath.Add(point);
            }
            while (index > 0);

            // 删除多个小角度路线之间的路径点，以精简路径点。
            index = 1;
            while (index + 2 < _vectorPath.Count)
            {
                Vector2 p1 = _vectorPath[index - 1];
                Vector2 p2 = _vectorPath[index];
                Vector2 p3 = _vectorPath[index + 1];
                Vector2 p4 = _vectorPath[index + 2];

                if ((p2 - p3).sqrMagnitude < Utils.Sqr(2.2f))
                {
                    Vector2 vector12 = (p2 - p1).normalized;
                    Vector2 vector23 = (p3 - p2).normalized;
                    Vector2 vector34 = (p4 - p3).normalized;
                    vector12 = new Vector3(vector12.x, 0f, vector12.y);
                    vector23 = new Vector3(vector23.x, 0f, vector23.y);
                    vector34 = new Vector3(vector34.x, 0f, vector34.y);

                    float cross13 = Vector3.Cross(vector12, vector23).y;
                    float cross24 = Vector3.Cross(vector23, vector34).y;

                    // 备注：sin45度 约等于 0.707f
                    if (cross13 * cross24 > 0f && Mathf.Abs(cross13) < 0.71f && Mathf.Abs(cross24) < 0.71f)
                    {
                        Segment segment12 = new Segment(p1, p2);
                        Segment segment34 = new Segment(p3, p4);
                        if (segment12.LineIntersection(segment34, out Vector2 intersection)
                            && _blockMap.LinecastCached(p1, intersection, _unitSizeInBlock, _movementMask)
                            && _blockMap.LinecastCached(intersection, p4, _unitSizeInBlock, _movementMask))
                        {
                            _vectorPath[index] = intersection;
                            _vectorPath.RemoveAt(index + 1);
                            index--;
                        }
                    }
                }

                index++;
            }
        }

        /// <summary>
        /// 向开放列表添加节点。
        /// </summary>
        /// <param name="blockMapPoint"></param>
        /// <param name="g"></param>
        /// <param name="depth"></param>
        /// <param name="direction"></param>
        /// <returns>返回该节点的H值。</returns>
        private float AddOpenNode(Vector2Int blockMapPoint, float g, int depth, Direction direction)
        {
            PathNode pathNode = PathNodeManager.GetPathNode();
            pathNode.Position = blockMapPoint;
            pathNode.G = g;
            pathNode.H = Vector2Int.Distance(blockMapPoint, _endPos) * HEURISTIC_SCALE;
            _openList.Add(pathNode);

            int index = GetIndex(pathNode.Position);
            VisitedNode[] visitedNodes = _blockMap.VisitedNodes;
            visitedNodes[index].OpenNodeIndex = pathNode.GlobalIndex;
            visitedNodes[index].NodeState = NodeState.Walkable;
            visitedNodes[index].Direction = direction;

            return pathNode.H;
        }

        private Vector2Int GetNearestValidPoint(Vector2Int blockMapPoint)
        {
            Vector2Int destination = blockMapPoint;
            int distanceSqr = int.MaxValue;
            int mapLength = Mathf.Max(_blockMap.SizeX, _blockMap.SizeZ);
            bool result = false;

            for (int i = 1; i < mapLength; i++)
            {
                // 右下向上检测
                Vector2Int tempStart = new Vector2Int(blockMapPoint.x + i, blockMapPoint.y - i);
                Vector2Int tempEnd = new Vector2Int(blockMapPoint.x + i, blockMapPoint.y + i);
                result |= LineDetect(tempStart, tempEnd, true, ref distanceSqr, ref destination);
                // 左下向上检测
                tempStart.x = blockMapPoint.x - i;
                tempEnd.x = blockMapPoint.x - i;
                result |= LineDetect(tempStart, tempEnd, true, ref distanceSqr, ref destination);
                // 左上向右检测
                tempStart.y = blockMapPoint.y + i;
                tempStart.x = blockMapPoint.x - i + 1;
                tempEnd.y = blockMapPoint.y + i;
                tempEnd.x = blockMapPoint.x + i - 1;
                result |= LineDetect(tempStart, tempEnd, false, ref distanceSqr, ref destination);
                // 左下向右检测
                tempStart.y = blockMapPoint.y - i;
                tempEnd.y = blockMapPoint.y - i;
                result |= LineDetect(tempStart, tempEnd, false, ref distanceSqr, ref destination);

                if (result)
                    break;
            }

            return destination;

            bool LineDetect(Vector2Int start, Vector2Int end, bool isVertical, ref int distanceSqr, ref Vector2Int destination)
            {
                Vector2Int tempDestination = start;
                bool result = false;
                if (isVertical)
                {
                    if (!IsXValid(tempDestination))
                        return result;

                    while (tempDestination.y <= end.y)
                    {
                        if (IsZValid(tempDestination) && !_blockMap.IsBlockRestricted(GetIndex(tempDestination), _unitSizeInBlock, _movementMask))
                        {
                            int currentDistSqr = Utils.SqrDistance(tempDestination, blockMapPoint);
                            if (currentDistSqr < distanceSqr)
                            {
                                distanceSqr = currentDistSqr;
                                destination = tempDestination;
                                result = true;
                            }
                        }
                        tempDestination.y++;
                    }
                    return result;
                }
                else
                {
                    if (!IsZValid(tempDestination))
                        return result;

                    while (tempDestination.x <= end.x)
                    {
                        if (IsXValid(tempDestination) && !_blockMap.IsBlockRestricted(GetIndex(tempDestination), _unitSizeInBlock, _movementMask))
                        {
                            int currentDistSqr = Utils.SqrDistance(tempDestination, blockMapPoint);
                            if (currentDistSqr < distanceSqr)
                            {
                                distanceSqr = currentDistSqr;
                                destination = tempDestination;
                                result = true;
                            }
                        }
                        tempDestination.x++;
                    }
                    return result;
                }
            }
        }

        public Vector3 FindNearestValidPoint(Vector3 position, int unitSizeInWorld, BlockFlag movementMask)
        {
            _unitSizeInBlock = _blockMap.WorldToBlockSize(unitSizeInWorld);
            _movementMask = movementMask;
            Vector2 posInWorld = new Vector3(position.x, position.z);
            Vector2Int posInBlock = _blockMap.WorldToBlockPoint(posInWorld, _unitSizeInBlock);
            int posInIndex = _blockMap.BlockToIndex(posInBlock);
            
            if (_blockMap.IsBlockRestricted(posInIndex, _unitSizeInBlock, _movementMask))
            {
                posInBlock = GetNearestValidPoint(posInBlock);
                posInWorld = _blockMap.BlockToWorldPoint(posInBlock, _unitSizeInBlock);
                
                return new Vector3(posInWorld.x, 0f, posInWorld.y);
            }

            return position;
        }

        public Vector3 ProjectToNearestValidPoint(Vector3 from, Vector3 to, int unitSizeInWorld, BlockFlag movementMask)
        {
            int unitSizeInBlock = _blockMap.WorldToBlockSize(unitSizeInWorld);
            Vector2Int fromInBlock = _blockMap.WorldToBlockPoint(new Vector2(from.x, from.z), unitSizeInBlock);
            Vector2Int toInBlock = _blockMap.WorldToBlockPoint(new Vector2(to.x, to.z), unitSizeInBlock);
            
            Vector2Int hitPoint = ProjectToNearestValidPoint(fromInBlock, toInBlock, unitSizeInBlock, movementMask);
            Vector2 nearestInWorld = _blockMap.BlockToWorldPoint(hitPoint, unitSizeInBlock);
            return new Vector3(nearestInWorld.x, 0f, nearestInWorld.y);
        }

        private Vector2Int ProjectToNearestValidPoint(Vector2Int from, Vector2Int to, int unitSizeInBlock, BlockFlag movementMask)
        {
            Vector2 dir = to - from;
            int length = Mathf.RoundToInt(dir.magnitude / PROJECT_STEP);
            Vector2 step = dir.normalized * PROJECT_STEP;
            Vector2 temp = from;

            for (int i = 0; i < length; i++)
            {
                if (!_blockMap.IsBlockRestricted(_blockMap.BlockToIndex(from), unitSizeInBlock, movementMask))
                {
                    return from;
                }

                temp += step;
                from = new Vector2Int(Mathf.RoundToInt(temp.x), Mathf.RoundToInt(temp.y));
            }
            
            return to;
        }

        private int GetIndex(Vector2Int blockMapPoint)
        {
            return blockMapPoint.x + blockMapPoint.y * _blockMap.SizeX;
        }

        private bool IsXValid(Vector2Int blockMapPoint)
        {
            return blockMapPoint.x >= 0 && blockMapPoint.x <= _blockMap.SizeX - _unitSizeInBlock;
        }

        private bool IsZValid(Vector2Int blockMapPoint)
        {
            return blockMapPoint.y >= 0 && blockMapPoint.y <= _blockMap.SizeZ - _unitSizeInBlock;
        }

        private bool IsBlockMapPointValid(Vector2Int blockMapPoint)
        {
            return IsXValid(blockMapPoint) && IsZValid(blockMapPoint);
        }

        internal struct VisitedNode
        {
            // 与PathNodeManager，从节点池中节点所在的索引一致，与节点所在位置无关。
            internal int OpenNodeIndex { get; set; }
            internal NodeState NodeState { get; set; }
            internal Direction Direction { get; set; }
        }

        internal class PathNode : IHeapItem<PathNode>
        {
            private int _globalIndex;

            public int GlobalIndex => _globalIndex;
            public int HeapIndex { get; set; }
            public float G { get; set; }
            public float H { get; set; }
            public float F => G + H;
            public int Depth { get; set; }
            public Vector2Int Position { get; set; }

            public PathNode(int globalIndex)
            {
                _globalIndex = globalIndex;
            }

            public int CompareTo(PathNode other)
            {
                return F.CompareTo(other.F);
            }
        }

        internal enum NodeState
        {
            Unvisited,
            Walkable,
            Blocked
        }

        [Flags]
        internal enum Direction : byte
        {
            None = 0,
            Left = 1,
            Right = 2,
            Up = 4,
            Down = 8,
            UpLeft = 5,
            UpRight = 6,
            DownLeft = 9,
            DownRight = 10
        }

        internal enum PathResult
        {
            Success,
            FailStart,
            Fail
        }
    }
}

