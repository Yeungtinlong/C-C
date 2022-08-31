using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CNC.PathFinding.BlockMapSO;
using static CNC.PathFinding.Path;

namespace CNC.PathFinding
{
    internal class AStarPathGenerator
    {
        private PathDriver _driver;
        private BlockMapSO _blockMap;
        private bool _isPathValid;
        private PathResult _pathResult;
        private List<Vector2> _currentPath;

        public bool IsPathValid => _isPathValid;
        public PathResult PathResult => _pathResult;
        public List<Vector2> CurrentPath => _currentPath;

        internal AStarPathGenerator(PathDriver driver, BlockMapSO blockMap)
        {
            _driver = driver;
            _blockMap = blockMap;
        }

        internal void Reset()
        {
            _isPathValid = false;
        }

        internal void RequestPath(Vector3 from, Vector3 to, float maxRange)
        {
            Path path = new Path(_blockMap);
            Vector2 start = new Vector2(from.x, from.z);
            Vector2 end = new Vector2(to.x, to.z);
            path.FindPath(start, end, _driver.UnitSize, maxRange, GetMovementFlags(_driver.Damageable.UnitType));

            _isPathValid = path.Result == PathResult.Success ? true : false;
            _currentPath = path.VectorPath;
            _pathResult = path.Result;
        }
    }
}

