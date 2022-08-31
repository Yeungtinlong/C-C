using UnityEngine;

namespace CNC.PathFinding
{
    [CreateAssetMenu(fileName = "SurfaceMapSO", menuName = "Path Finding/SurfaceMapSO")]
    public class SurfaceMapSO : ScriptableObject
    {
        [SerializeField] private BlockMapSO _blockMapSO = default;

        private int _surfaceMapWidth;
        private int _surfaceMapSize;
        private float _surfaceMapScale;

        public void Initialize(int surfaceMapWidth, int surfaceMapSize, float surfaceMapScale)
        {
            _surfaceMapWidth = surfaceMapWidth;
            _surfaceMapSize = surfaceMapSize;
            _surfaceMapScale = surfaceMapScale;
            InitBlockMap();
        }

        private void InitBlockMap()
        {
            _blockMapSO.Initialize(_surfaceMapWidth / _surfaceMapScale, _surfaceMapWidth / _surfaceMapScale, 1f / _surfaceMapScale);
        }
    }
}