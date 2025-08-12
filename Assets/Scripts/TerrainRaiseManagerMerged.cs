using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 입자들이 쌓인 형태를 깊이(bakeRadius)를 가진 구 형태로 Terrain에 베이크하는 매니저.
/// 일정 간격으로 속도 및 위치 필터링된 입자를 Terrain에 반영 후 즉시 제거합니다.
/// 스탬핑 시 선형 페일오프를 적용하고, 각 영역에 박스 블러 스무딩을 수행합니다.
/// </summary>
public class TerrainRaiseManagerMerged : MonoBehaviour, IPoolable
{
    [Header("Terrain Reference")]
    [SerializeField] private Terrain terrain;

    [Header("Bake Settings")]
    [SerializeField, Tooltip("자동 베이크 활성화 여부")] private bool autoBakeEnabled = true;
    [SerializeField, Tooltip("자동 베이크 간격(초)")] private float autoBakeInterval = 5f;
    [SerializeField, Tooltip("지형 위 이 높이(m) 이상 떠 있는 파티클은 베이크 제외")] private float maxBakeHeightAboveGround = 0.5f;
    [SerializeField, Tooltip("베이크 대상 속도 임계값 (m/s)")] private float bakeVelocityThreshold = 0.1f;

    [Header("Particle Bake Defaults")]
    [SerializeField, Tooltip("파티클에 bakeRadius가 없을 때 사용할 기본 반경(m)")] private float defaultBakeRadius = 0.5f;
    [SerializeField, Tooltip("파티클에 heightOffset이 없을 때 사용할 기본 높이 오프셋(m)")] private float defaultHeightOffset = 1.5f;

    [Header("Auto Destroy Settings")]
    [SerializeField, Tooltip("터레인 아래로 떨어진 입자는 즉시 삭제")] private float destroyBelowOffset = 0.1f;

    [Header("VFX Settings")]
    [SerializeField, Tooltip("흙이 쌓일 때 생성될 먼지 VFX 프리팹")] private GameObject dustVFXPrefab;

    [Header("Slope Relaxation Settings")]
    [SerializeField] private float relaxRadius = 2f;
    [SerializeField] private float maxSlopeAngleDeg = 35f;
    [SerializeField] private float relaxStrength = 0.01f;

    private TerrainData _terrainData;
    private float _bakeTimer = 0f;
    private GameObjectPool _pool;

    void Awake()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        _terrainData = terrain.terrainData;
    }

    void Update()
    {
        if (!autoBakeEnabled) return;
        _bakeTimer += Time.deltaTime;
        if (_bakeTimer >= autoBakeInterval)
        {
            _bakeTimer = 0f;
            BakeAndClearEligibleParticles();
        }
    }

    private void BakeAndClearEligibleParticles()
    {
        var eligible = new List<SoilParticleMerged>();
        float velThreshSqr = bakeVelocityThreshold * bakeVelocityThreshold;
        Vector3 tPos = terrain.transform.position;

        // ← 여기에 GrabbedParticle 태그 스킵 로직 추가
        foreach (var sp in FindObjectsOfType<SoilParticleMerged>())
        {
            if (sp.gameObject.CompareTag("GrabbedParticle"))
                continue;   // 버킷에 잡힌 입자는 제외

            var rb = sp.GetComponent<Rigidbody>();
            if (rb.velocity.sqrMagnitude > velThreshSqr)
                continue;

            Vector3 wpos = sp.transform.position;
            float surfaceY = terrain.SampleHeight(wpos) + tPos.y;

            if (wpos.y < surfaceY - destroyBelowOffset)
            {
                Destroy(sp.gameObject);
                continue;
            }
            if (wpos.y > surfaceY + maxBakeHeightAboveGround)
                continue;

            eligible.Add(sp);
        }

        if (eligible.Count == 0) return;

        BakeHeightMap(eligible);
        PaintTextureMap(eligible);

        foreach (var sp in eligible)
        {
            if (sp == null) continue;
            var go = sp.gameObject;
            var col = go.GetComponent<Collider>(); if (col) col.enabled = false;
            var rb = go.GetComponent<Rigidbody>(); if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            go.SetActive(false);
            Destroy(go);
        }
    }

    private void BakeHeightMap(List<SoilParticleMerged> particles)
    {
        int res = _terrainData.heightmapResolution;
        float[,] heights = _terrainData.GetHeights(0, 0, res, res);
        Vector3 tPos = terrain.transform.position;
        float mapX = _terrainData.size.x, mapZ = _terrainData.size.z, mapY = _terrainData.size.y;
        float cellX = mapX / (res - 1), cellZ = mapZ / (res - 1);

        var maxHeights = new Dictionary<Vector2Int, float>();

        foreach (var sp in particles)
        {
            Vector3 wpos = sp.transform.position;
            float surfaceY = terrain.SampleHeight(wpos) + tPos.y;
            float offset = sp.heightOffset > 0f ? sp.heightOffset : defaultHeightOffset;
            float bakeY = surfaceY + offset;
            float normT = Mathf.Clamp01((bakeY - tPos.y) / mapY);

            Vector3 local = wpos - tPos;
            int cx = Mathf.RoundToInt((local.x / mapX) * (res - 1));
            int cz = Mathf.RoundToInt((local.z / mapZ) * (res - 1));
            var key = new Vector2Int(cx, cz);

            if (!maxHeights.ContainsKey(key) || maxHeights[key] < normT)
                maxHeights[key] = normT;
        }

        foreach (var kv in maxHeights)
        {
            int cx = kv.Key.x, cz = kv.Key.y;
            float normT = kv.Value;
            float rad = defaultBakeRadius, rr = rad * rad;
            int rX = Mathf.CeilToInt(rad / cellX), rZ = Mathf.CeilToInt(rad / cellZ);

            int x0 = Mathf.Clamp(cx - rX, 0, res - 1), x1 = Mathf.Clamp(cx + rX, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rZ, 0, res - 1), z1 = Mathf.Clamp(cz + rZ, 0, res - 1);

            for (int z = z0; z <= z1; z++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) * cellX;
                    float dz = (z - cz) * cellZ;
                    float dist2 = dx * dx + dz * dz;
                    if (dist2 <= rr)
                    {
                        float dist = Mathf.Sqrt(dist2);
                        float weight = 1f - (dist / rad);
                        float current = heights[z, x];
                        float target = Mathf.Lerp(current, normT, weight);
                        heights[z, x] = Mathf.Max(current, target);
                    }
                }
            }

            // 경사 완화
            RelaxSlope(heights, res, cx, cz, cellX, cellZ);

            // 스무딩 필터 적용
            int width = x1 - x0 + 1;
            int height = z1 - z0 + 1;
            SmoothRegion(x0, z0, width, height, heights);
        }

        _terrainData.SetHeights(0, 0, heights);
    }

    private void RelaxSlope(float[,] heights, int res, int cx, int cz, float cellX, float cellZ)
    {
        int rPx = Mathf.CeilToInt(relaxRadius / cellX);
        int x0 = Mathf.Clamp(cx - rPx, 1, res - 2), x1 = Mathf.Clamp(cx + rPx, 1, res - 2);
        int z0 = Mathf.Clamp(cz - rPx, 1, res - 2), z1 = Mathf.Clamp(cz + rPx, 1, res - 2);
        float maxSlope = Mathf.Tan(maxSlopeAngleDeg * Mathf.Deg2Rad);

        for (int z = z0; z <= z1; z++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float ddz = (heights[z + 1, x] - heights[z - 1, x]) / (2 * cellZ);
                float ddx = (heights[z, x + 1] - heights[z, x - 1]) / (2 * cellX);
                float slope = Mathf.Sqrt(ddx * ddx + ddz * ddz);
                if (slope > maxSlope)
                {
                    heights[z, x] -= (slope - maxSlope) * relaxStrength;
                }
            }
        }
    }

    private void SmoothRegion(int startX, int startZ, int width, int height, float[,] heights)
    {
        var copy = new float[height, width];
        for (int dz = 0; dz < height; dz++)
            for (int dx = 0; dx < width; dx++)
                copy[dz, dx] = heights[startZ + dz, startX + dx];

        for (int dz = 1; dz < height - 1; dz++)
        {
            for (int dx = 1; dx < width - 1; dx++)
            {
                float sum = 0f;
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                        sum += copy[dz + oy, dx + ox];
                heights[startZ + dz, startX + dx] = sum / 9f;
            }
        }
    }

    private void PaintTextureMap(List<SoilParticleMerged> particles)
    {
        int res = _terrainData.alphamapResolution;
        int layers = _terrainData.alphamapLayers;
        float[,,] alphas = _terrainData.GetAlphamaps(0, 0, res, res);
        Vector3 tPos = terrain.transform.position;
        float mapX = _terrainData.size.x, mapZ = _terrainData.size.z;
        float cellX = mapX / (res - 1), cellZ = mapZ / (res - 1);
        foreach (var sp in particles)
        {
            var layer = sp.GetLayer();
            int li = System.Array.IndexOf(_terrainData.terrainLayers, layer);
            if (li < 0) continue;
            Vector3 wpos = sp.transform.position;
            Vector3 local = wpos - tPos;
            int cx = Mathf.RoundToInt(local.x / mapX * (res - 1));
            int cz = Mathf.RoundToInt(local.z / mapZ * (res - 1));
            float rad = sp.bakeRadius * 1.5f;
            int rX = Mathf.CeilToInt(rad / cellX), rZ = Mathf.CeilToInt(rad / cellZ);
            for (int z = Mathf.Clamp(cz - rZ, 0, res - 1); z <= Mathf.Clamp(cz + rZ, 0, res - 1); z++)
                for (int x = Mathf.Clamp(cx - rX, 0, res - 1); x <= Mathf.Clamp(cx + rX, 0, res - 1); x++)
                {
                    float dx = (x - cx) * cellX, dz = (z - cz) * cellZ;
                    if (dx * dx + dz * dz > rad * rad) continue;
                    float t = 1f - Mathf.Sqrt(dx * dx + dz * dz) / rad;
                    float sum = 0;
                    for (int l = 0; l < layers; l++) { alphas[z, x, l] = l == li ? Mathf.Lerp(alphas[z, x, l], 1f, t) : Mathf.Lerp(alphas[z, x, l], 0f, t); sum += alphas[z, x, l]; }
                    for (int l = 0; l < layers; l++) alphas[z, x, l] /= sum;
                }
        }
        _terrainData.SetAlphamaps(0, 0, alphas);
    }

    // IPoolable 구현
    public void SetPoolInstance(GameObjectPool poolInstance) => _pool = poolInstance;
    public bool ComparePoolInstance(GameObjectPool poolInstance) => _pool == poolInstance;
}