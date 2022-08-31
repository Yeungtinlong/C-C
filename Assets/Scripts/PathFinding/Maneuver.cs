using CNC.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.PathFinding
{
    internal abstract class Maneuver
    {
        protected int _maneuverID;
        protected int _localRouteID;
        protected int _priority;
        protected int _bypassNode;

        protected Vector2 _entryPoint;
        protected Vector2 _entryDirection;
        protected Vector2 _exitPoint;
        protected Vector2 _exitDirection;
        protected float _length;
        protected List<Maneuver> _linkedManeuvers = new List<Maneuver>();

        internal int ManeuverID { get => _maneuverID; set { _maneuverID = value; } }
        internal int LocalRouteID { get => _localRouteID; set { _localRouteID = value; } }
        internal int Priority { get => _priority; set { _priority = value; } }
        internal int BypassNode => _bypassNode;
        internal Vector2 EntryPoint => _entryPoint;
        internal Vector2 EntryDirection => _entryDirection;
        internal Vector2 ExitPoint => _exitPoint;
        internal Vector2 ExitDirection => _exitDirection;
        internal float Length => _length;

        internal void Draw()
        {

        }

        internal void LinkManeuver(Maneuver maneuver)
        {
            if (maneuver == null)
                return;

            _linkedManeuvers.Add(maneuver);
        }

        internal bool CheckNonLinkedManeuver(Maneuver maneuver)
        {
            return _linkedManeuvers.FindIndex(linkedManeuver => linkedManeuver == maneuver) < 0;
        }

        internal abstract void Step(float stepLength, ref float finishedPercentage, out Vector2 resultPoint, out Vector2 resultDirection);
        
        internal virtual float GetRemainingLength(float finishedPercentage)
        {
            return _length * (1f - finishedPercentage);
        }
    }

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

    internal struct ManeuverInfo
    {
        public Vector2 EntryPoint { get; set; }
        public Vector2 NextPoint { get; set; }
        public Vector2 FollowPoint { get; set; }
        public Vector2 EntryDirection { get; set; }
        public Vector2 TargetDirection { get; set; }
    }
}

