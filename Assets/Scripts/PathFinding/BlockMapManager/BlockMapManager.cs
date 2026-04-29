namespace CNC.PathFinding
{
    public static class BlockMapManager
    {
        private static IBlockMapManager _singleton;

        public static IBlockMapManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    _singleton = new BlockMapManagerInternal();
                }

                return _singleton;
            }
        }
    }
}