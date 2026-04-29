namespace CNC.PathFinding
{
    public struct DriverState
    {
        public DriverInfo DriverInfo { get; set; }
        public float ProxySpeed { get; set; }
        public float ProxyAcceleration { get; set; }
        public bool IsBraking { get; set; }
        public bool IsGiveUp { get; set; }
        public ArrivalState ArrivalState { get; set; }
    }
}