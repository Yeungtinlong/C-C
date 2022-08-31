using CNC.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VisibilitySystemSO", menuName = "VisibilitySystem/Visibility System")]
public class VisibilitySystemSO : ScriptableObject
{
    [SerializeField] private float _visibilityResolution = default;
    [SerializeField] private LevelLimitSO _levelLimitSO = default;

    private float _visMapScale;
    private int _visMapWidth;
    private int _visMapSize;

    private byte[] _currentVisMap;
    private byte[][] _teamVisMaps;
    private byte[] _clearBuffer;

    private float[] _visHeightMap;
    private float[] _visDensityMap;

    private Texture2D _rawTexture;
    private Texture2D _lastTexture;
    private byte[] _lastTextureBuffer;

    private FactionType _currentUpdateFaction;
    private FactionType _localPlayerFaction;

    private int _subFrame = -1;

    private const float TARGET_UNIT_HEIGHT = 2f;
    private const int UPDATE_FRAMES = 10;

    public Texture2D FogOfWarTexture2D => _rawTexture;
    public Texture2D LastFogOfWarTexture2D => _lastTexture;

    public void Initialize(float mapWidth, FactionType localPlayerFaction)
    {
        _localPlayerFaction = localPlayerFaction;

        _visMapScale = 1f * _visibilityResolution;
        _visMapWidth = Mathf.FloorToInt(mapWidth * _visMapScale);
        _visMapSize = _visMapWidth * _visMapWidth;

        _clearBuffer = new byte[_visMapWidth];

        _currentVisMap = new byte[_visMapSize];
        _teamVisMaps = new byte[3][];
        _teamVisMaps[0] = new byte[_visMapSize];
        _teamVisMaps[1] = new byte[_visMapSize];
        _teamVisMaps[2] = new byte[_visMapSize];
        _lastTextureBuffer = new byte[_visMapSize];

        _rawTexture = new Texture2D(_visMapWidth, _visMapWidth, TextureFormat.R8, false);
        _rawTexture.wrapMode = TextureWrapMode.Clamp;
        _rawTexture.hideFlags = HideFlags.DontSave;

        _lastTexture = new Texture2D(_visMapWidth, _visMapWidth, TextureFormat.R8, false);
        _lastTexture.wrapMode = TextureWrapMode.Clamp;
        _lastTexture.hideFlags = HideFlags.DontSave;

        _visHeightMap = new float[_visMapSize];
        _visDensityMap = new float[_visMapSize];
    }

    public void UpdateUnitView(Controllable controllable)
    {
        if (controllable.Damageable.UnitID % 10 == _subFrame &&
            controllable.Damageable.Faction == _currentUpdateFaction)
        {
            float sightRange = controllable.GetSightRange() * _visMapScale;
            if (sightRange >= 1f)
            {
                SightPoint[] sightPoints = controllable.GetSightPoints();

                if (sightPoints != null)
                {
                    // �����õ�λ���� sightPoint���ҳ��� sightPoint λ�ú��Ӿࡣ
                    for (int i = 0; i < sightPoints.Length; i++)
                    {
                        Vector3 pos = sightPoints[i].Position;
                        UnitVisCache visCache = sightPoints[i].VisCache;

                        int x = (int) (pos.x * _visMapScale);
                        int z = (int) (pos.z * _visMapScale);

                        // ��� sightPoint ��λ���뻺��Ĳ�һ�£���λ�÷����˸ı䣬����Ҫ������Ұ��
                        if (visCache.X != x || visCache.Y != z || visCache.Range != (int) sightRange)
                        {
                            DrawUnitToVisCacheWithOcclusion(visCache, new Vector2Int(x, z), sightRange, pos.y);
                        }

                        CopyVisFromCache(visCache);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    public void ShowHeightMapDebug()
    {
        float toWorldScale = 1f / _visMapScale;

        for (int i = 0; i < _visHeightMap.Length; i++)
        {
            int x = i % _visMapWidth;
            int z = i / _visMapWidth;
            Vector3 transformPos = new Vector3(x * toWorldScale + 0.5f * toWorldScale, 0.01f,
                z * toWorldScale + 0.5f * toWorldScale);
            Gizmos.color = _visHeightMap[i] > 500f ? Color.red : Color.green;
            Gizmos.DrawCube(transformPos, new Vector3(0.9f * toWorldScale, 0.01f, 0.9f * toWorldScale));
        }
    }
#endif

    public void UpdateFogOfWar()
    {
        if (_subFrame == -1)
            ClearImage();

        _subFrame++;

        // ÿ10֡����ĳһ�ӵ���Ұ��
        if (_subFrame == UPDATE_FRAMES)
        {
            Utils.Swap(ref _currentVisMap, ref _teamVisMaps[(int) _currentUpdateFaction]);

            _subFrame = -1;

            if (_currentUpdateFaction == _localPlayerFaction)
                LoadTeamVisToTexture(_localPlayerFaction);

            _currentUpdateFaction++;

            if (_currentUpdateFaction > FactionType.Nod)
                _currentUpdateFaction = FactionType.GDI;
        }
    }

    public void CreateMapBorder(LevelRectSO levelRect)
    {
        int minX = (int) Mathf.Clamp(levelRect.MinX * _visMapScale, 0f, _visMapWidth);
        int minZ = (int) Mathf.Clamp(levelRect.MinZ * _visMapScale, 0f, _visMapWidth);
        int maxX = (int) Mathf.Clamp(levelRect.MaxX * _visMapScale, 0f, _visMapWidth);
        int maxZ = (int) Mathf.Clamp(levelRect.MaxZ * _visMapScale, 0f, _visMapWidth);

        for (int i = 0; i < _visMapWidth; i++)
        {
            for (int j = 0; j < _visMapWidth; j++)
            {
                if (j < minX || j >= maxX || i < minZ || i >= maxZ)
                {
                    _visHeightMap[j + i * _visMapWidth] += 1000f;
                }
            }
        }
    }

    public void UpdateMapBorder(LevelRectSO levelRect)
    {
        int minX = (int) Mathf.Clamp(levelRect.MinX * _visMapScale, 0f, _visMapWidth - 1);
        int minZ = (int) Mathf.Clamp(levelRect.MinZ * _visMapScale, 0f, _visMapWidth - 1);
        int maxX = (int) Mathf.Clamp(levelRect.MaxX * _visMapScale, 0f, _visMapWidth - 1);
        int maxZ = (int) Mathf.Clamp(levelRect.MaxZ * _visMapScale, 0f, _visMapWidth - 1);

        for (int i = minZ; i < maxZ; i++)
        {
            for (int j = minX; j < maxX; j++)
            {
                if (_visHeightMap[j + i * _visMapWidth] > 500f)
                {
                    _visHeightMap[j + i * _visMapWidth] -= 1000f;
                }
            }
        }
    }

    public bool IsPointVisibleByLocalPlayer(Vector3 transformPoint)
    {
        return IsPointVisibleByFaction(transformPoint, _localPlayerFaction);
    }

    public bool IsPointVisibleByFaction(Vector3 transformPoint, FactionType faction)
    {
        int x = Mathf.FloorToInt(transformPoint.x * _visMapScale);
        int y = Mathf.FloorToInt(transformPoint.z * _visMapScale);
        return x > 0 && x < _visMapWidth && y > 0 && y < _visMapWidth &&
               _teamVisMaps[(int) faction][x + y * _visMapWidth] > 128;
    }

    private void LoadTeamVisToTexture(FactionType faction)
    {
        _lastTexture.LoadRawTextureData(_lastTextureBuffer);
        _lastTexture.Apply();
        Array.Copy(_teamVisMaps[(int) faction], _lastTextureBuffer, _teamVisMaps[(int) faction].Length);
        _rawTexture.LoadRawTextureData(_teamVisMaps[(int) faction]);
        _rawTexture.Apply();
    }

    private void CopyVisFromCache(UnitVisCache visCache)
    {
        int range = visCache.Range;
        int cacheWidth = 2 * range + 1;

        int xStartCache = 0;
        int xEndCache = cacheWidth;

        int yStartCache = 0;
        int yEndCache = cacheWidth;

        int xStartInMap = visCache.X - range;
        int yStartInMap = visCache.Y - range;

        // �жϻ�����Ұ���ұ߽��Ƿ���ڵ�ͼ�߽硣
        if (xStartInMap + xEndCache > _visMapWidth)
        {
            xEndCache -= xStartInMap + xEndCache - _visMapWidth;
        }

        // �жϻ�����Ұ����߽��Ƿ�С�ڵ�ͼ�߽硣
        if (xStartInMap < 0)
        {
            int overflow = -xStartInMap;
            xStartCache += overflow;
            xStartInMap += overflow;
            xEndCache -= overflow;
        }

        if (yStartInMap + yEndCache > _visMapWidth)
        {
            yEndCache -= yStartInMap + yEndCache - _visMapWidth;
        }

        if (yStartInMap < 0)
        {
            int overflow = -yStartInMap;
            yStartCache += overflow;
            yStartInMap += overflow;
            yEndCache -= overflow;
        }

        int startPosCache = xStartCache + yStartCache * cacheWidth;
        int startPosInMap = xStartInMap + yStartInMap * _visMapWidth;

        for (int i = 0; i < yEndCache; i++)
        {
            int curPosCache = startPosCache;
            int curPos = startPosInMap;
            for (int j = 0; j < xEndCache; j++)
            {
                byte[] currentVisMap = _currentVisMap;
                currentVisMap[curPos] |= visCache.Map[curPosCache];
                curPosCache++;
                curPos++;
            }

            startPosCache += cacheWidth;
            startPosInMap += _visMapWidth;
        }
    }

    /// <summary>
    /// ������׶���޳��󣬽���λ��ӽ����ӷ�Χ�����С�
    /// </summary>
    /// <param name="visCache"></param>
    /// <param name="blockPoint"></param>
    /// <param name="range"></param>
    /// <param name="viewHeight"></param>
    private void DrawUnitToVisCacheWithOcclusion(UnitVisCache visCache, Vector2Int blockPoint, float range,
        float viewHeight)
    {
        int blockRange = (int) range;
        int cacheWidth = 2 * blockRange + 1; // ������Ϊ�е㣬ǰ����Ͼ�����Ϊ��Ұ���εı߳���
        int cacheSize = cacheWidth * cacheWidth; // ��Ұ���ε������

        int unitX = blockPoint.x;
        int unitY = blockPoint.y;

        visCache.X = unitX;
        visCache.Y = unitY;
        visCache.Range = (int) range;

        if (visCache.Map != null && visCache.Map.Length == cacheSize)
            Array.Clear(visCache.Map, 0, cacheSize);
        else
            visCache.Map = new byte[cacheSize];

        UnitVisDrawer unitVisDrawer = new UnitVisDrawer
        {
            Horizon = new float[(int) range + 1],
            Density = new float[(int) range + 1],
            Range = range,
            ViewHeight = viewHeight,
            SrcHeight = _visHeightMap,
            SrcDensity = _visDensityMap,
            Dest = visCache.Map
        };

        int srcPos = unitX + unitY * _visMapWidth;
        int destPos = blockRange + blockRange * cacheWidth;

        int xCount = _visMapWidth - unitX - 1;
        int yCount = _visMapWidth - unitY - 1;

        // �� dest �е�λ���ڵĵ���Ϊ255�ɼ���dest ������Ϻ󽫻Ḳ�ǵ� visCache��
        unitVisDrawer.Dest[destPos] = byte.MaxValue;

        unitVisDrawer.DrawPieSlice(srcPos, 1, _visMapWidth, xCount, yCount, destPos, 1, cacheWidth);
        unitVisDrawer.DrawPieSlice(srcPos, _visMapWidth, 1, yCount, xCount, destPos, cacheWidth, 1);

        unitVisDrawer.DrawPieSlice(srcPos, -_visMapWidth, 1, unitY, xCount, destPos, -cacheWidth, 1);
        unitVisDrawer.DrawPieSlice(srcPos, 1, -_visMapWidth, xCount, unitY, destPos, 1, -cacheWidth);

        unitVisDrawer.DrawPieSlice(srcPos, -1, -_visMapWidth, unitX, unitY, destPos, -1, -cacheWidth);
        unitVisDrawer.DrawPieSlice(srcPos, -_visMapWidth, -1, unitY, unitX, destPos, -cacheWidth, -1);

        unitVisDrawer.DrawPieSlice(srcPos, _visMapWidth, -1, yCount, unitX, destPos, cacheWidth, -1);
        unitVisDrawer.DrawPieSlice(srcPos, -1, _visMapWidth, unitX, yCount, destPos, -1, cacheWidth);
    }

    private void ClearImage()
    {
        for (int i = 0; i < _visMapSize; i += _visMapWidth)
        {
            Array.Copy(_clearBuffer, 0, _currentVisMap, i, _visMapWidth);
        }
    }

    private struct UnitVisDrawer
    {
        public float[] Horizon { get; set; }
        public float[] Density { get; set; }
        public float Range { get; set; }
        public float ViewHeight { get; set; }
        public float[] SrcHeight { get; set; }
        public float[] SrcDensity { get; set; }
        public byte[] Dest { get; set; }

        public void DrawPieSlice(int srcPos, int srcXStep, int srcYStep, int xLimit, int yLimit, int destPos,
            int destXStep, int destYStep)
        {
            int yCount = Mathf.Min((int) Range, yLimit);

            srcPos += srcYStep;
            destPos += destYStep;

            Horizon[1] = SrcHeight[srcPos + srcXStep] - ViewHeight;
            Density[1] = SrcDensity[srcPos + srcXStep];
            Dest[destPos + destXStep] = byte.MaxValue;
            Horizon[0] = SrcHeight[srcPos] - ViewHeight;
            Density[0] = SrcDensity[srcPos];
            Dest[destPos] = byte.MaxValue;

            for (int i = 2; i <= yCount; i++)
            {
                int xCount = Mathf.Min(Mathf.Min(i, (int) Mathf.Sqrt(Range * Range - i * i)), xLimit);

                // ˥��ϵ������ i==2 ʱ���Ϊ2���� i ��������ֵ�ӽ�1��ģ��͸�ӽ���ԶС��Ӱ�졣
                // ����ע�⣬������������ʱ����תΪ������������õ��Ľ���ᶪʧ����С����
                float attenuationScale = (float) i / (float) (i - 1);
                float reciprocalOfI = 1f / i;

                srcPos += srcYStep;
                int targetPos = srcPos + xCount * srcXStep;
                destPos += destYStep;
                int targetPosInDest = destPos + xCount * destXStep;

                // ɨ�����η�Χ�ĵ㣬j ���� x �����ƫ�ơ�
                for (int j = xCount; j > 0; j--)
                {
                    // ���۲��ĸ߶ȣ���ȥ�۲��߸߶ȣ�����Ϊ��ƽ�߿�����
                    float targetHeight = SrcHeight[targetPos] - ViewHeight;
                    float lerpT = j * reciprocalOfI;

                    // ��ֵ����ڵ���߶ȡ�
                    float obstacleHeight = Mathf.LerpUnclamped(Horizon[j], Horizon[j - 1], lerpT);
                    float densityScale = Mathf.LerpUnclamped(Density[j], Density[j - 1], lerpT);
                    float sqrDistance = i * i + j * j;

                    obstacleHeight *= attenuationScale;
                    densityScale = (densityScale * (i - 1) + SrcDensity[targetPos]) * reciprocalOfI;

                    // ����1����Ұ�߶ȱ�Ŀ��ߣ�
                    // ����2������������ǶȺ�Ŀ������Ұ��Χ�ڣ�
                    // ��������������Ŀ��ɼ�����ֵΪ255��
                    if (obstacleHeight < targetHeight + TARGET_UNIT_HEIGHT &&
                        sqrDistance * densityScale * densityScale < Range * Range)
                    {
                        Dest[targetPosInDest] = byte.MaxValue;
                    }

                    // ���Ŀ���߶ȱȽϸߣ���ôĿ����Ϊ�µ��ڵ��㡣
                    Horizon[j] = Mathf.Max(obstacleHeight, targetHeight);
                    Density[j] = densityScale;

                    targetPos -= srcXStep;
                    targetPosInDest -= destXStep;
                }

                // ɨ��ֱ�߷���y����ĵ㡣

                // ��ʱviewPos�� y ���ϡ�
                float straightTargetHeight = SrcHeight[targetPos] - ViewHeight;
                float straightObstacleHeight = Horizon[0];
                float straightDensityScale = Density[0];
                float straightSqrDistance = i * i;

                straightObstacleHeight *= attenuationScale;
                straightDensityScale = (straightDensityScale * (i - 1) + SrcDensity[targetPos]) * reciprocalOfI;
                
                if (straightObstacleHeight < straightTargetHeight + TARGET_UNIT_HEIGHT &&
                    straightSqrDistance * straightDensityScale * straightDensityScale < Range * Range)
                {
                    Dest[targetPosInDest] = byte.MaxValue;
                }

                Horizon[0] = Mathf.Max(straightObstacleHeight, straightTargetHeight);
                Density[0] = straightDensityScale;
            }
        }
    }

    public class SightPoint
    {
        public Vector3 Position { get; set; }
        public UnitVisCache VisCache { get; set; } = new UnitVisCache();
    }

    public class UnitVisCache
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Range { get; set; }
        public byte[] Map { get; set; }
    }
}