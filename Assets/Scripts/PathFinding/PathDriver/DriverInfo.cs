using UnityEngine;

namespace CNC.PathFinding
{
    public struct DriverInfo
    {
        internal Vector2 Position { get; set; }
        internal Vector2 Rotation { get; set; }
        internal float Speed { get; set; }

        internal void Reset(Vector2 position, Vector2 rotation)
        {
            Position = position;
            Rotation = rotation;
            Speed = 0f;
        }
    }
}