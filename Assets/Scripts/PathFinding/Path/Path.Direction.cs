using System;

namespace CNC.PathFinding
{
    public partial class Path
    {
        [Flags]
        internal enum Direction : byte
        {
            None = 0,
            Left = 1,
            Right = 2,
            Up = 4,
            Down = 8,
            UpLeft = 5,
            UpRight = 6,
            DownLeft = 9,
            DownRight = 10
        }
    }
}