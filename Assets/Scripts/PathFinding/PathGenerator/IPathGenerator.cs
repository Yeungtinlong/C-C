using System.Collections.Generic;
using UnityEngine;

namespace CNC.PathFinding
{
    public interface IPathGenerator
    {
        public bool IsPathValid { get; }
        public Path.PathResult PathResult { get; }
        public List<Vector2> CurrentPath { get; }

        public void RequestPath(Vector3 from, Vector3 to, float maxRange);

        public void Reset();
    }
}