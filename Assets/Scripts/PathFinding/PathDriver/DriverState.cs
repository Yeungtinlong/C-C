namespace CNC.PathFinding
{
    public struct DriverState
    {
        internal DriverInfo DriverInfo { get; set; }
        internal float ProxySpeed { get; set; }
        internal float ProxyAcceleration { get; set; }
        internal bool IsBraking { get; set; }
        internal bool IsGiveUp { get; set; }
        internal ArrivalState ArrivalState { get; set; }
    }
}