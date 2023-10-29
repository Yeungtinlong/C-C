using UnityEngine;

namespace CNC.PathFinding
{
    internal class Straight : Maneuver
    {
        protected Vector2 _moveDirection;
        internal Vector2 MoveDirection => _moveDirection;

        public Straight(Vector2 entryPoint, Vector2 exitPoint, int bypassNode)
        {
            _entryPoint = entryPoint;
            _exitPoint = exitPoint;
            _exitDirection = (exitPoint - entryPoint).normalized;
            _bypassNode = bypassNode;
            _priority = 0;

            _moveDirection = (exitPoint - entryPoint).normalized;
            _length = (_exitPoint - _entryPoint).magnitude;
        }

        internal override void Step(float stepLength, ref float finishedPercentage, out Vector2 resultPoint, out Vector2 resultDirection)
        {
            finishedPercentage += stepLength / _length;
            resultDirection = _exitDirection;
            resultPoint = _entryPoint + _moveDirection * (_length * finishedPercentage + stepLength);
        }
    }
}