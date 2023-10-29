using CNC.Utility;
using UnityEngine;

namespace CNC.PathFinding
{
    internal class InPlaceTurn : Maneuver
    {
        protected float _phi;
        protected float _turnRadius;

        internal float Phi => _phi;
        internal float TurnRadius => _turnRadius;

        public InPlaceTurn(Vector2 entryPoint, Vector2 entryDirection, Vector2 exitDirection, float turnRadius, int bypassNode)
        {
            _entryDirection = entryDirection;   // 半径方向
            _entryPoint = entryPoint;
            _exitPoint = entryPoint;
            _turnRadius = turnRadius;
            _bypassNode = bypassNode;
            _priority = 0;

            _phi = Vector2.SignedAngle(entryDirection, exitDirection);
            _length = Mathf.Abs(turnRadius * _phi * Mathf.Deg2Rad);
        }

        internal override void Step(float stepLength, ref float finishedPercentage, out Vector2 resultPoint, out Vector2 resultDirection)
        {
            float finishedDegrees = finishedPercentage * _phi + Mathf.Sign(_phi) * (stepLength / _turnRadius) * Mathf.Rad2Deg;
            finishedPercentage = finishedDegrees / _phi;
            resultPoint = _exitPoint;
            resultDirection = Utils.Rotate(_entryDirection, finishedDegrees);
        }
    }
}