using UnityEngine;

namespace CNC.PathFinding
{
    public partial class Path
    {
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
    }
}