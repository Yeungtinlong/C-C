namespace CNC.PathFinding
{
    public partial class Path
    {
        public struct VisitedNode
        {
            // 与PathNodeManager，从节点池中节点所在的索引一致，与节点所在位置无关。
            public int OpenNodeIndex { get; set; }
            public NodeState NodeState { get; set; }
            public Direction Direction { get; set; }
        }
    }
}