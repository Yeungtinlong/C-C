namespace CNC.PathFinding.Proximity
{
    public class ProximityManager
    {
        public static IProximityManager _singleton;
        
        public static IProximityManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    _singleton = new ProximityManagerInternal();
                }

                return _singleton;
            }
        }
    }
}