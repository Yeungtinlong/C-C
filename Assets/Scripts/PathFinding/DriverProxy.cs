using static CNC.PathFinding.PathDriver;
using UnityEngine;

namespace CNC.PathFinding
{
    internal class DriverProxy
    {
        private Vector2 _position;

        internal int FrameID { get; set; }
        internal int ManeuverID { get; set; }
        internal int ManeuverIndex { get; set; }
        internal float ManeuverPercentage { get; set; }

        internal ArrivalState ArrivalState { get; set; }
        internal DriverProxy Previous { get; set; }
        internal DriverProxy Next { get; set; }

        internal Vector2 Position
        {
            get => _position;
            set
            {
                ProxyVelocity = (value - _position) / Time.deltaTime;
                _position = value;
            }
        }

        internal Vector2 Orientation { get; set; }
        internal float ProxySpeed { get; set; }
        internal float ProxyAcceleration { get; set; }
        internal Vector2 ProxyVelocity { get; set; }
        internal bool IsBraking { get; set; }

        internal void Reset(int frameID, Vector2 worldPoint, Vector2 orientation)
        {
            FrameID = frameID;
            ManeuverID = 0;
            ManeuverIndex = 0;
            ManeuverPercentage = 0f;

            Position = worldPoint;
            Orientation = orientation;
            ProxySpeed = 0f;
            ProxyAcceleration = 0f;
            ProxyVelocity = Vector2.zero;
            ArrivalState = ArrivalState.FreeMoving;
            IsBraking = false;
        }

        internal void SetNextFrom(DriverProxy previous)
        {
            FrameID = previous.FrameID + 1;
            ManeuverID = previous.ManeuverID;
            ManeuverIndex = previous.ManeuverIndex;
            ManeuverPercentage = previous.ManeuverPercentage;

            Position = previous.Position;
            Orientation = previous.Orientation;
            ProxySpeed = previous.ProxySpeed;
            ProxyAcceleration = previous.ProxyAcceleration;
            ProxyVelocity = previous.ProxyVelocity;
            ArrivalState = previous.ArrivalState;
            IsBraking = previous.IsBraking;
        }

        internal void ResetManeuver()
        {
            ManeuverID = 0;
            ManeuverIndex = 0;
            ManeuverPercentage = 0f;
        }

        internal bool IsMoving()
        {
            return ProxySpeed > 0f || ProxyAcceleration > 0f;
        }
    }
}

