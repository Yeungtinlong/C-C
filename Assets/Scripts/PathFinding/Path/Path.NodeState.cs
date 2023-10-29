namespace CNC.PathFinding
{
    public partial class Path
    {
        internal enum NodeState
        {
            Unvisited,
            Walkable,
            Blocked
        }
    }
}