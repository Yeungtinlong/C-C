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

    internal struct ManeuverInfo
    {
        public Vector2 EntryPoint { get; set; }
        public Vector2 NextPoint { get; set; }
        public Vector2 FollowPoint { get; set; }
        public Vector2 EntryDirection { get; set; }
        public Vector2 TargetDirection { get; set; }
    }
}

