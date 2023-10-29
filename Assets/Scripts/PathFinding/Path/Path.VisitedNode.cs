namespace CNC.PathFinding
{
    public partial class Path
    {
        internal struct VisitedNode
        {
            // 与PathNodeManager，从节点池中节点所在的索引一致，与节点所在位置无关。
            internal int OpenNodeIndex { get; set; }
            internal NodeState NodeState { get; set; }
            internal Direction Direction { get; set; }
        }
    }
}