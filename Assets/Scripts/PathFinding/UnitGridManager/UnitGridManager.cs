namespace CNC.PathFinding.UnitGrid
{
    public static class UnitGridManager
    {
        private static IUnitGridManager _singleton;

        public static IUnitGridManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    _singleton = new UnitGridManagerInternal();
                }

                return _singleton;
            }
        }
    }
}