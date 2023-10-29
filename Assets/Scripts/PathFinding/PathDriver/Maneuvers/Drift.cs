using CNC.Utility;
using UnityEngine;

namespace CNC.PathFinding
{
    internal class Drift : Maneuver
    {
        protected float _linearLength;
        protected float _turnRadius;
        protected float _phi;   // 转向角
        protected Vector2 _moveDirection;

        internal float LinearLength => _linearLength;
        internal float TurnRadius => _turnRadius;
        internal float Phi => _phi;
        internal Vector2 MoveDirection => _moveDirection;

        protected Drift() { }

        internal Drift(Vector2 entryPoint, Vector2 entryDirection, Vector2 exitPoint, Vector2 exitDirection, float turnRadius, int bypassNode)
        {
            _entryPoint = entryPoint;
            _entryDirection = entryDirection;
            _exitPoint = exitPoint;
            _exitDirection = exitDirection;
            _turnRadius = turnRadius;
            _bypassNode = bypassNode;
            _priority = 0;

            _phi = Vector2.SignedAngle(entryDirection, exitDirection);
            _moveDirection = (_exitPoint - _entryPoint).normalized;
            _linearLength = (_exitPoint - _entryPoint).magnitude;
            // 可能存在转向角为0，则取直线距离。
            _length = Mathf.Max(turnRadius * _phi * Mathf.Deg2Rad, _linearLength);
        }

        internal override void Step(float stepLength, ref float finishedPercentage, out Vector2 resultPoint, out Vector2 resultDirection)
        {
            finishedPercentage += stepLength / _length;
            float turnedDegree = _phi * finishedPercentage;
            resultDirection = Utils.Rotate(_entryDirection, turnedDegree);
            resultPoint = _entryPoint + _moveDirection * _linearLength * finishedPercentage;
        }
    }
}