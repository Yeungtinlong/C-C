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
                    // 遍历该单位所有 sightPoint，找出各 sightPoint 位置和视距。
                    for (int i = 0; i < sightPoints.Length; i++)
                    {
                        Vector3 pos = sightPoints[i].Position;
                        UnitVisCache visCache = sightPoints[i].VisCache;

                        int x = (int) (pos.x * _visMapScale);
                        int z = (int) (pos.z * _visMapScale);

                        // 如果 sightPoint 的位置与缓存的不一致，即位置发生了改变，就需要更新视野。
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

        // 每10帧更新某一队的视野。
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

        // 判断缓存视野的右边界是否大于地图边界。
        if (xStartInMap + xEndCache > _visMapWidth)
        {
            xEndCache -= xStartInMap + xEndCache - _visMapWidth;
        }

        // 判断缓存视野的左边界是否小于地图边界。
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
    /// 经过视锥体剔除后，将单位添加进可视范围缓存中。
    /// </summary>
    /// <param name="visCache"></param>
    /// <param name="blockPoint"></param>
    /// <param name="range"></param>
    /// <param name="viewHeight"></param>
    private void DrawUnitToVisCacheWithOcclusion(UnitVisCache visCache, Vector2Int blockPoint, float range,
        float viewHeight)
    {
        int blockRange = (int) range;
        int cacheWidth = 2 * blockRange + 1; // 以自身为中点，前后加上距离作为视野方形的边长。
        int cacheSize = cacheWidth * cacheWidth; // 视野方形的面积。

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

        // 将 dest 中单位所在的点设为255可见，dest 计算完毕后将会覆盖到 visCache。
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

                // 衰减系数：在 i==2 时最大，为2，随 i 的增长，值接近1，模拟透视近大远小的影响。
                // 必须注意，在做整数除法时必须转为浮点数，否则得到的结果会丢失所有小数。
                float attenuationScale = (float) i / (float) (i - 1);
                float reciprocalOfI = 1f / i;

                srcPos += srcYStep;
                int targetPos = srcPos + xCount * srcXStep;
                destPos += destYStep;
                int targetPosInDest = destPos + xCount * destXStep;

                // 扫描扇形范围的点，j 是在 x 方向的偏移。
                for (int j = xCount; j > 0; j--)
                {
                    // 被观察点的高度，减去观察者高度，折算为地平线看出。
                    float targetHeight = SrcHeight[targetPos] - ViewHeight;
                    float lerpT = j * reciprocalOfI;

                    // 插值算出遮挡物高度。
                    float obstacleHeight = Mathf.LerpUnclamped(Horizon[j], Horizon[j - 1], lerpT);
                    float densityScale = Mathf.LerpUnclamped(Density[j], Density[j - 1], lerpT);
                    float sqrDistance = i * i + j * j;

                    obstacleHeight *= attenuationScale;
                    densityScale = (densityScale * (i - 1) + SrcDensity[targetPos]) * reciprocalOfI;

                    // 条件1：视野高度比目标高；
                    // 条件2：折算空气浑浊度后，目标在视野范围内；
                    // 满足上述条件，目标可见，设值为255。
                    if (obstacleHeight < targetHeight + TARGET_UNIT_HEIGHT &&
                        sqrDistance * densityScale * densityScale < Range * Range)
                    {
                        Dest[targetPosInDest] = byte.MaxValue;
                    }

                    // 如果目标点高度比较高，那么目标点成为新的遮挡点。
                    Horizon[j] = Mathf.Max(obstacleHeight, targetHeight);
                    Density[j] = densityScale;

                    targetPos -= srcXStep;
                    targetPosInDest -= destXStep;
                }

                // 扫描直线方向，y方向的点。

                // 此时viewPos在 y 轴上。
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