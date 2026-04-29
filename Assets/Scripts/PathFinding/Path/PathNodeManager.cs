using System.Collections.Generic;
using UnityEngine;
using static CNC.PathFinding.Path;

internal static class PathNodeManager
{
    private static readonly VisitedNode[] _visitedNodes = new VisitedNode[MAP_SIZE];
    private static readonly Stack<PathNode> _pathNodePool = new Stack<PathNode>();
    private static readonly List<PathNode> _pathNodeLookupByIndex = new List<PathNode>();
    private static readonly Heap<PathNode> _openList = new Heap<PathNode>();
    
    private const int SIZE_X = 2048;
    private const int SIZE_Z = 2048;
    public const int MAP_SIZE = 4194304;

    internal static VisitedNode[] VisitedNodes => _visitedNodes;
    internal static Heap<PathNode> OpenList => _openList;

    static PathNodeManager()
    {
        _pathNodeLookupByIndex.Add(null);
    }

    internal static PathNode GetPathNode()
    {
        if (_pathNodePool.Count > 0)
            return _pathNodePool.Pop();

        int count = _pathNodeLookupByIndex.Count;
        PathNode pathNode = new PathNode(count);
        _pathNodeLookupByIndex.Add(pathNode);
        if (pathNode == null)
        {
            Debug.Log("null pathNode.");
        }        
        return pathNode;
    }

    internal static PathNode GetPathNodeByIndex(int index)
    {
        return _pathNodeLookupByIndex[index];
    }

    internal static void ReleasePathNode(PathNode pathNode)
    {
        _pathNodePool.Push(pathNode);
    }
}
