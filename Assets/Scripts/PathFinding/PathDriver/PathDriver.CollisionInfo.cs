using UnityEngine;

namespace CNC.PathFinding
{
    public partial class PathDriver
    {
        internal struct CollisionInfo
        {
            internal Vector2 EntryPosition { get; set; }
            internal Vector2 EntryVelocity { get; set; }
            internal float CollisionSpeed { get; set; }

            internal Maneuver ManeuverInfo { get; set; }
            internal int ManeuverIndex { get; set; }
            internal int CollisionFrameID { get; set; }
            internal int EvaluationFrameID { get; set; }
            internal int EvasionFrameID { get; set; }

            internal IPathDriver TargetDriver { get; set; }
            internal IPathDriver LastDriver { get; set; }
            internal Vector2 TargetPosition { get; set; }
            internal int MaxManeuverPriority { get; set; }
            internal bool StaticDriver { get; set; }

            internal int CollisionPreventionFrameID { get; set; }
            internal int StopFrameID { get; set; }
            internal int DecelerationFrames { get; set; }
            internal int IgnoreCounter { get; set; }

            internal bool IsProcessed { get; set; }

            internal void Reset()
            {
                ManeuverInfo = null;
                ManeuverIndex = -1;
                CollisionSpeed = 0f;
                CollisionFrameID = -1;
                EvaluationFrameID = -1;
                EvasionFrameID = -1;

                TargetDriver = null;
                StopFrameID = -1;
                DecelerationFrames = 0;
                MaxManeuverPriority = 0;
                StaticDriver = false;

                IsProcessed = false;
            }

            internal static void Update(CollisionInfo newInfo, ref CollisionInfo oldInfo)
            {
                IPathDriver driver = newInfo.TargetDriver;
                int ignoreCounter = newInfo.IgnoreCounter;

                if (driver == oldInfo.LastDriver)
                    ignoreCounter++;
                else
                    ignoreCounter = 0;

                oldInfo = newInfo;
                oldInfo.LastDriver = driver;
                oldInfo.IgnoreCounter = ignoreCounter;
            }

            internal bool NeedProcessing()
            {
                return IsAvailable() && !IsProcessed;
            }

            internal bool IsAvailable()
            {
                return TargetDriver != null || StaticDriver;
            }
        }
    }
}