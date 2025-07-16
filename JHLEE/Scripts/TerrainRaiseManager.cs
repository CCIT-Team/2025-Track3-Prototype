using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainRaiseManager : MonoBehaviour
{
    [Header("Terrain Reference")]
    [SerializeField] private Terrain terrain;

    [Header("Bake Settings")]
    [SerializeField] private bool autoBakeEnabled = true;
    [SerializeField] private float autoBakeInterval = 5f;

    [Header("Particle Bake Defaults")]
    [SerializeField] private float defaultBakeRadius = 0.5f;
    [SerializeField] private float defaultHeightOffset = 0.3f;

    [Header("Slope Relaxation Settings")]
    [SerializeField] private float relaxRadius = 2f;
    [SerializeField] private float maxSlopeAngleDeg = 35f;
    [SerializeField] private float relaxStrength = 0.01f;

    private TerrainData _terrainData;
    private TerrainCollider _terrainCollider;
    private List<GameObject> _stopped = new List<GameObject>();
    private float _bakeTimer = 0f;

    void Awake()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        _terrainData = terrain.terrainData;
        _terrainCollider = terrain.GetComponent<TerrainCollider>();
    }

    public void RegisterStop(GameObject particle)
    {
        if (particle == null || _stopped.Contains(particle)) 
            return;

        // 단순히 리스트에 추가만!
        _stopped.Add(particle);
    }

    void Update()
    {
        if (!autoBakeEnabled) return;
        _bakeTimer += Time.deltaTime;
        if (_bakeTimer >= autoBakeInterval)
        {
            _bakeTimer = 0f;
            BakeAndClearParticles();
        }
    }

    private void BakeAndClearParticles()
    {
        if (_stopped.Count == 0) 
            return;

        // 1) Terrain 베이크만 수행
        BakeTerrainFromParticles();

        // 2) 베이크된 입자 일괄 파괴
        foreach (var go in _stopped)
            if (go != null)
                Destroy(go);

        _stopped.Clear();
    }

    private void BakeTerrainFromParticles()
    {
        int res = _terrainData.heightmapResolution;
        float[,] heights = _terrainData.GetHeights(0, 0, res, res);
        Vector3 tPos = terrain.transform.position;
        float mapSizeX = _terrainData.size.x;
        float mapSizeZ = _terrainData.size.z;
        float mapSizeY = _terrainData.size.y;
        float cellSizeX = mapSizeX / (res - 1);
        float cellSizeZ = mapSizeZ / (res - 1);

        var centers = new HashSet<Vector2Int>();

        foreach (var go in _stopped)
        {
            if (go == null) continue;
            var sp = go.GetComponent<SoilParticle>();
            float radius = sp?.bakeRadius ?? defaultBakeRadius;
            float centerY = go.transform.position.y + (sp?.heightOffset ?? defaultHeightOffset);
            float normTarget = Mathf.Clamp01((centerY - tPos.y) / mapSizeY);

            Vector3 localPos = go.transform.position - tPos;
            int cx = Mathf.RoundToInt(localPos.x / mapSizeX * (res - 1));
            int cz = Mathf.RoundToInt(localPos.z / mapSizeZ * (res - 1));

            centers.Add(new Vector2Int(cx, cz));

            int rX = Mathf.CeilToInt(radius / cellSizeX);
            int rZ = Mathf.CeilToInt(radius / cellSizeZ);

            int x0 = Mathf.Clamp(cx - rX, 0, res - 1);
            int x1 = Mathf.Clamp(cx + rX, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rZ, 0, res - 1);
            int z1 = Mathf.Clamp(cz + rZ, 0, res - 1);

            float rr = radius * radius;

            for (int z = z0; z <= z1; z++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) * cellSizeX;
                    float dz = (z - cz) * cellSizeZ;
                    if (dx * dx + dz * dz > rr) continue;
                    float cellY = heights[z, x] * mapSizeY + tPos.y;
                    float dy = go.transform.position.y - cellY;
                    if (dy * dy + dx * dx + dz * dz > rr) continue;
                    if (heights[z, x] < normTarget) heights[z, x] = normTarget;
                }
            }

        }

        foreach (var p in centers)
            RelaxSlopeAround(heights, res, p.x, p.y, cellSizeX, cellSizeZ, mapSizeY);

        _terrainData.SetHeights(0, 0, heights);
    }

    private void RelaxSlopeAround(float[,] heights, int res, int cx, int cz,
                                   float cellSizeX, float cellSizeZ, float mapSizeY)
    {
        int radiusPx = Mathf.RoundToInt(relaxRadius / cellSizeX);
        int x0 = Mathf.Clamp(cx - radiusPx, 1, res - 2);
        int x1 = Mathf.Clamp(cx + radiusPx, 1, res - 2);
        int z0 = Mathf.Clamp(cz - radiusPx, 1, res - 2);
        int z1 = Mathf.Clamp(cz + radiusPx, 1, res - 2);

        float maxSlope = Mathf.Tan(maxSlopeAngleDeg * Mathf.Deg2Rad);

        for (int z = z0; z <= z1; z++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dz = (heights[z + 1, x] - heights[z - 1, x]) / (2 * cellSizeZ);
                float dx = (heights[z, x + 1] - heights[z, x - 1]) / (2 * cellSizeX);
                float slope = Mathf.Sqrt(dx * dx + dz * dz);

                if (slope > maxSlope)
                {
                    float excess = slope - maxSlope;
                    float reduce = excess * relaxStrength;

                    heights[z, x] -= reduce;
                    float disperse = reduce * 0.25f;
                    heights[z + 1, x] += disperse;
                    heights[z - 1, x] += disperse;
                    heights[z, x + 1] += disperse;
                    heights[z, x - 1] += disperse;
                }
            }
        }

        // ─── 추가: 박스 블러 (3×3) ───
        int w = x1 - x0 + 1, h = z1 - z0 + 1;
        float[,] copy = new float[h, w];
        // 원본 영역 복사
        for (int dz = 0; dz < h; dz++)
            for (int dx = 0; dx < w; dx++)
                copy[dz, dx] = heights[z0 + dz, x0 + dx];

        // 블러 적용
        for (int dz = 1; dz < h - 1; dz++)
        {
            for (int dx = 1; dx < w - 1; dx++)
            {
                float sum = 0f;
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                        sum += copy[dz + oy, dx + ox];
                heights[z0 + dz, x0 + dx] = sum / 9f;
            }
        }
    }
}
