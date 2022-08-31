using UnityEngine;
using CNC.PathFinding;
using CNC.Utility;

public class TerrainManager : MonoBehaviour
{
    [SerializeField] private UnitGridSO _unitGridSO = default;
    [SerializeField] private UnitProximitySO _unitProximitySO = default;
    [SerializeField] private SurfaceMapSO _surfaceMapSO = default;
    [SerializeField] private BlockMapSO _blockMapSO = default;
    [SerializeField] private VisibilitySystemSO _visibilitySystemSO = default;

    [SerializeField] private int _surfaceMapWidthScale = 2;

    private Terrain _terrain;
    private int _terrainWidth;
    private int _surfaceMapSize;
    private int _surfaceMapWidth;
    private float _surfaceMapScale;
    
#if UNITY_EDITOR
    [SerializeField] private bool _debugMode = default;
    [SerializeField] private bool _showBlockMap = default;
    [SerializeField] private bool _showUnitGrid = default;
    [SerializeField] private bool _showProximity = default;
    [SerializeField] private bool _showHeightMap = default;

    private void OnDrawGizmos()
    {
        if (!_debugMode)
            return;

        if (_showBlockMap)
        {
            _blockMapSO.ShowBlocksInDebugMode();
        }
        
        if (_showUnitGrid)
            _unitGridSO.ShowGridsInDebugMode();

        if (_showProximity)
            _unitProximitySO.ShowBlocksInDebugMode();

        if (_showHeightMap)
        {
            _visibilitySystemSO.ShowHeightMapDebug();
        }
    }
#endif

    [SerializeField] private GameObject _fogOfWarPlane = default;
    private void Awake()
    {
        PrepareTerrain();
        PrepareUnitProximity();
        PrepareSurfaceMap();
        PrepareUnitGrid();
        PrepareVisibilitySystem();
        _fogOfWarPlane.SetActive(true);
    }

    private void Update()
    {
        _visibilitySystemSO.UpdateFogOfWar();
    }

    private void PrepareTerrain()
    {
        _terrain = GetComponent<Terrain>();
        _terrainWidth = (int)_terrain.terrainData.size.x;
        _surfaceMapWidth = _terrainWidth * _surfaceMapWidthScale;
        _surfaceMapSize = Utils.Sqr(_surfaceMapWidth);
        _surfaceMapScale = (float)_surfaceMapWidth / (float)_terrainWidth;
    }

    private void PrepareUnitProximity()
    {
        _unitProximitySO.Initialize(_terrainWidth);
    }

    private void PrepareSurfaceMap()
    {
        _surfaceMapSO.Initialize(_surfaceMapWidth, _surfaceMapSize, _surfaceMapScale);
    }

    private void PrepareUnitGrid()
    {
        _unitGridSO.Initialize(_terrainWidth);
    }

    private void PrepareVisibilitySystem()
    {
        _visibilitySystemSO.Initialize(_terrainWidth, PlayerInfo.Instance.FactionType);
    }
}
