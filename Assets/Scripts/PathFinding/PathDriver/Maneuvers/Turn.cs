using CNC.Utility;
using UnityEngine;

namespace CNC.PathFinding
{
    internal class Turn : Maneuver
    {
        protected float _turnRadius;
        protected Vector2 _center;
        protected Vector2 _radiusDirection;
        protected float _phiRad;

        internal float TurnRadius => _turnRadius;
        internal Vector2 Center => _center;
        internal Vector2 RadiusDirection => _radiusDirection;
        internal float PhiRad => _phiRad;   // 圆心角弧度

        public Turn(Vector2 entryPoint, Vector2 exitPoint, Vector2 center, float phiRad, float turnRadius, int bypassNode)
        {
            _entryPoint = entryPoint;
            _exitPoint = exitPoint;
            _center = center;
            _phiRad = phiRad;   // 可能为负数，视方向而定。TODO: 未判断正负
            _turnRadius = turnRadius;
            _bypassNode = bypassNode;
            _priority = 1;

            _length = Mathf.Abs(turnRadius * phiRad);
            _radiusDirection = (entryPoint - center).normalized;
            Vector2 exitRadiusDirection = Utils.Rotate(_radiusDirection, _phiRad * Mathf.Rad2Deg);
            _exitDirection = Utils.Perpendicular(exitRadiusDirection, Mathf.Sign(_phiRad));
        }

        internal override void Step(float stepLength, ref float finishedPercentage, out Vector2 resultPoint, out Vector2 resultDirection)
        {
            float finishedRad = finishedPercentage * _phiRad + Mathf.Sign(_phiRad) * stepLength / _turnRadius;
            finishedPercentage = finishedRad / _phiRad;
            Vector2 resultRadiusDirection = Utils.Rotate(_radiusDirection, finishedRad * Mathf.Rad2Deg);
            resultDirection = Utils.Perpendicular(resultRadiusDirection, Mathf.Sign(_phiRad));
            resultPoint = _center + resultRadiusDirection * _turnRadius;
        }
    }
}