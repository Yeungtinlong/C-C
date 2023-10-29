namespace CNC.PathFinding
{
    internal enum ArrivalState
    {
        FreeMoving,
        Arrive,
        Stop,
        StopWithInertia,
        GiveUp,
        Complete
    }
}