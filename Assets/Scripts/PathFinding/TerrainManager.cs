using UnityEngine;
using CNC.PathFinding;
using CNC.PathFinding.Proximity;
using CNC.PathFinding.UnitGrid;
using CNC.Utility;

public class TerrainManager : MonoBehaviour
{
    [SerializeField] private SurfaceMapSO _surfaceMapSO = default;
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
            BlockMapManager.Singleton.ShowBlocksInDebugMode();
        }
        
        if (_showUnitGrid)
            UnitGridManager.Singleton.ShowGridsInDebugMode();

        if (_showProximity)
            ProximityManager.Singleton.ShowBlocksInDebugMode();

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
        ProximityManager.Singleton.Initialize(_terrainWidth);
        
        // _unitProximitySO.Initialize(_terrainWidth);
    }

    private void PrepareSurfaceMap()
    {
        _surfaceMapSO.Initialize(_surfaceMapWidth, _surfaceMapSize, _surfaceMapScale);
    }

    private void PrepareUnitGrid()
    {
        UnitGridManager.Singleton.Initialize(_terrainWidth);
    }

    private void PrepareVisibilitySystem()
    {
        _visibilitySystemSO.Initialize(_terrainWidth, PlayerInfo.Instance.FactionType);
    }
}
