using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 흙 입자를 Terrain에 '올려 쌓는' 베이크 매니저(안정화 버전; 풀 의존성 제거).
/// - 속도/고도 필터로 엘리지블 파티클 선별
/// - 셀당 최대 상승량 제한(팝/흡착 완화)
/// - 버킷/암 등 보호 레이어 주변은 raise 금지(Protect Zone)
/// - 베이크 직후 보호 대상과 Terrain 충돌 잠시 무시(튐/흡착 완화)
/// - 풀을 쓰지 않아도 컴파일 가능(Despawn은 SetActive(false)→Destroy 대체)
/// </summary>
public class TerrainRaiseManagerMerged : MonoBehaviour
{
    [Header("Terrain Reference")]
    [SerializeField] private Terrain terrain;

    [Header("Auto Bake")]
    [SerializeField] private bool autoBakeEnabled = true;
    [SerializeField] private float autoBakeInterval = 5f;
    [SerializeField, Tooltip("지형 위 이 높이(m) 이상 떠 있는 파티클은 베이크 제외")]
    private float maxBakeHeightAboveGround = 0.5f;
    [SerializeField, Tooltip("베이크 대상 속도 임계값 (m/s)")]
    private float bakeVelocityThreshold = 0.1f;

    [Header("Particle Defaults")]
    [SerializeField, Tooltip("파티클에 bakeRadius가 없을 때 사용할 기본 반경(m)")]
    private float defaultBakeRadius = 0.5f;
    [SerializeField, Tooltip("파티클에 heightOffset이 없을 때 사용할 기본 높이 오프셋(m)")]
    private float defaultHeightOffset = 1.5f;

    [Header("Auto Destroy")]
    [SerializeField, Tooltip("터레인 아래로 떨어진 입자는 즉시 삭제")]
    private float destroyBelowOffset = 0.1f;

    [Header("VFX")]
    [SerializeField] private GameObject dustVFXPrefab;

    [Header("Slope Relax / Smoothing")]
    [SerializeField] private float relaxRadius = 2f;
    [SerializeField] private float maxSlopeAngleDeg = 35f;
    [SerializeField] private float relaxStrength = 0.01f;

    // ==== 안정화 추가 ====
    [Header("Stability / Protection")]
    [SerializeField, Tooltip("보호할 콜라이더 레이어(버킷/암 등)")]
    private LayerMask protectLayers;
    [SerializeField, Tooltip("보호 반경(m): 이 반경 내에선 raise 금지")]
    private float protectRadius = 0.35f;
    [SerializeField, Tooltip("베이크 1회당 셀 최대 상승량(m)")]
    private float maxRaisePerBake = 0.01f;
    [SerializeField, Tooltip("보호 반경 주변 충돌 무시 유지(초)")]
    private float ignoreCollisionTime = 0.25f;

    [Header("Despawn Policy")]
    [SerializeField, Tooltip("true면 우선 SetActive(false)로 비활성 → 못 찾으면 Destroy")]
    private bool preferDeactivateOverDestroy = true;

    private TerrainData _td;
    private TerrainCollider _terrainCol;
    private float _bakeTimer;

    // 베이크 직후 Terrain과 임시로 충돌무시할 목록
    private readonly List<(Collider col, float until)> _ignored = new();

    void Awake()
    {
        if (!terrain) terrain = Terrain.activeTerrain;
        if (!terrain)
        {
            Debug.LogError("[TerrainRaiseManagerMerged] Terrain 미할당");
            enabled = false; return;
        }
        _td = terrain.terrainData;
        _terrainCol = terrain.GetComponent<TerrainCollider>();
    }

    void Update()
    {
        RestoreExpiredIgnores();

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
        float vel2 = bakeVelocityThreshold * bakeVelocityThreshold;
        Vector3 tp = terrain.transform.position;

        foreach (var sp in FindObjectsOfType<SoilParticleMerged>())
        {
            if (sp == null || !sp.gameObject.activeInHierarchy) continue;
            if (sp.gameObject.CompareTag("GrabbedParticle")) continue; // 버킷에 잡힌 입자 제외

            var rb = sp.GetComponent<Rigidbody>();
            if (rb != null && rb.velocity.sqrMagnitude > vel2) continue;

            Vector3 wpos = sp.transform.position;
            float surfaceY = terrain.SampleHeight(wpos) + tp.y;

            // Terrain 아래로 빠진 건 즉시 제거/비활성
            if (wpos.y < surfaceY - destroyBelowOffset)
            {
                SafeDespawn(sp.gameObject);
                continue;
            }
            // 너무 높게 떠 있으면 보류
            if (wpos.y > surfaceY + maxBakeHeightAboveGround) continue;

            eligible.Add(sp);
        }

        if (eligible.Count == 0) return;

        // 보호 대상 수집(입자 범위 근처의 버킷/암 등)
        Bounds regionAABB = ComputeParticlesAABB(eligible, defaultBakeRadius);
        var protectCols = CollectProtectColliders(regionAABB, protectLayers);

        // 높이 반영(상승량 제한 + 보호구역 마스크)
        BakeHeightMap(eligible, protectCols);

        // 텍스처 페인트(원본 로직 유지)
        PaintTextureMap(eligible);

        // 베이크 직후 보호 대상과 Terrain 충돌 잠시 무시(흡착/튐 완화)
        ApplyTemporaryIgnores(protectCols, ignoreCollisionTime);

        // 파티클 제거/반환
        foreach (var sp in eligible)
        {
            if (sp == null) continue;
            SafeDespawn(sp.gameObject);
        }
    }

    // ---------- Height Bake with clamps & protection ----------
    private void BakeHeightMap(List<SoilParticleMerged> particles, List<Collider> protectCols)
    {
        int res = _td.heightmapResolution;
        float[,] heights = _td.GetHeights(0, 0, res, res);

        Vector3 tp = terrain.transform.position;
        Vector3 size = _td.size;
        float cellX = size.x / (res - 1);
        float cellZ = size.z / (res - 1);

        // 보호 원(2D XZ) 프리컴퓨트
        var protectCenters = new List<Vector2>();
        foreach (var c in protectCols)
        {
            if (c == null) continue;
            Vector3 p = c.bounds.center; // 대략 중심
            protectCenters.Add(new Vector2(p.x, p.z));
        }
        float protectR2 = protectRadius * protectRadius;

        // 셀별 목표 높이의 "최댓값"만 모은 뒤 → 적용 시 '상승량 제한'
        var maxHeights = new Dictionary<Vector2Int, float>();

        foreach (var sp in particles)
        {
            if (sp == null) continue;

            Vector3 wpos = sp.transform.position;
            float surfaceY = terrain.SampleHeight(wpos) + tp.y;
            float offset = (sp.heightOffset > 0f) ? sp.heightOffset : defaultHeightOffset;
            float bakeY = surfaceY + offset;
            float targetNorm = Mathf.Clamp01((bakeY - tp.y) / size.y);

            Vector3 local = wpos - tp;
            int cx = Mathf.RoundToInt((local.x / size.x) * (res - 1));
            int cz = Mathf.RoundToInt((local.z / size.z) * (res - 1));
            cx = Mathf.Clamp(cx, 0, res - 1);
            cz = Mathf.Clamp(cz, 0, res - 1);

            var key = new Vector2Int(cx, cz);
            if (!maxHeights.ContainsKey(key) || maxHeights[key] < targetNorm)
                maxHeights[key] = targetNorm;
        }

        float maxRaiseNorm = Mathf.Max(0.00001f, maxRaisePerBake / size.y);

        foreach (var kv in maxHeights)
        {
            int cx = kv.Key.x;
            int cz = kv.Key.y;
            float targetNorm = kv.Value;

            float rad = defaultBakeRadius;
            float rr = rad * rad;
            int rX = Mathf.CeilToInt(rad / cellX);
            int rZ = Mathf.CeilToInt(rad / cellZ);

            int x0 = Mathf.Clamp(cx - rX, 0, res - 1), x1 = Mathf.Clamp(cx + rX, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rZ, 0, res - 1), z1 = Mathf.Clamp(cz + rZ, 0, res - 1);

            for (int z = z0; z <= z1; z++)
            {
                float wz = tp.z + (z / (float)(res - 1)) * size.z;
                for (int x = x0; x <= x1; x++)
                {
                    float wx = tp.x + (x / (float)(res - 1)) * size.x;

                    // 보호 구역이면 raise 금지
                    if (IsInsideAnyProtect(wx, wz, protectCenters, protectR2))
                        continue;

                    float dx = (x - cx) * cellX;
                    float dz = (z - cz) * cellZ;
                    float d2 = dx * dx + dz * dz;
                    if (d2 > rr) continue;

                    float dist = Mathf.Sqrt(d2);
                    float weight = 1f - (dist / rad); // 선형 페이드(원본 느낌)
                    float cur = heights[z, x];
                    float want = Mathf.Lerp(cur, targetNorm, weight);

                    // 상승량 제한(셀당)
                    float delta = want - cur;
                    if (delta > 0f) delta = Mathf.Min(delta, maxRaiseNorm);
                    // 내리깎기는 하지 않음(이 매니저는 raise 전용)
                    heights[z, x] = cur + Mathf.Max(0f, delta);
                }
            }

            // 경사 완화 + 지역 스무딩
            RelaxSlope(heights, res, cx, cz, cellX, cellZ);
            int w = x1 - x0 + 1, h = z1 - z0 + 1;
            SmoothRegion(x0, z0, w, h, heights);
        }

        _td.SetHeights(0, 0, heights);
    }

    private static bool IsInsideAnyProtect(float wx, float wz, List<Vector2> centers, float r2)
    {
        if (centers == null || centers.Count == 0) return false;
        Vector2 p = new Vector2(wx, wz);
        for (int i = 0; i < centers.Count; i++)
        {
            if ((p - centers[i]).sqrMagnitude <= r2) return true;
        }
        return false;
    }

    // ---------- 보호 대상 수집 / 충돌 무시 ----------
    private static Bounds ComputeParticlesAABB(List<SoilParticleMerged> particles, float pad)
    {
        if (particles == null || particles.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
        var b = new Bounds(particles[0].transform.position, Vector3.one * 0.01f);
        for (int i = 1; i < particles.Count; i++)
            b.Encapsulate(particles[i].transform.position);
        b.Expand(pad * 2f);
        return b;
    }

    private static List<Collider> CollectProtectColliders(Bounds aabb, LayerMask layers)
    {
        var list = new List<Collider>();
        if (aabb.size.sqrMagnitude < 1e-6f) return list;

        Collider[] hits = Physics.OverlapBox(aabb.center, aabb.extents, Quaternion.identity, layers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c != null && !list.Contains(c)) list.Add(c);
        }
        return list;
    }

    private void ApplyTemporaryIgnores(List<Collider> cols, float duration)
    {
        if (_terrainCol == null || cols == null || cols.Count == 0) return;

        float until = Time.time + Mathf.Max(0.01f, duration);
        foreach (var c in cols)
        {
            if (c == null) continue;

            // 이미 등록되어 있으면 갱신
            bool exists = false;
            for (int i = 0; i < _ignored.Count; i++)
            {
                if (_ignored[i].col == c)
                {
                    _ignored[i] = (c, until);
                    exists = true; break;
                }
            }
            if (!exists)
            {
                Physics.IgnoreCollision(c, _terrainCol, true);
                _ignored.Add((c, until));
            }
        }
    }

    private void RestoreExpiredIgnores()
    {
        if (_terrainCol == null || _ignored.Count == 0) return;
        float now = Time.time;
        for (int i = _ignored.Count - 1; i >= 0; i--)
        {
            var (c, until) = _ignored[i];
            if (c == null || now >= until)
            {
                if (c != null) Physics.IgnoreCollision(c, _terrainCol, false);
                _ignored.RemoveAt(i);
            }
        }
    }

    // ---------- 기존 보조 로직 ----------
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
        if (width < 3 || height < 3) return;

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
        int res = _td.alphamapResolution;
        int layers = _td.alphamapLayers;
        float[,,] alphas = _td.GetAlphamaps(0, 0, res, res);

        Vector3 tp = terrain.transform.position;
        Vector3 size = _td.size;
        float cellX = size.x / (res - 1), cellZ = size.z / (res - 1);

        foreach (var sp in particles)
        {
            if (sp == null) continue;

            var layer = sp.GetLayer();
            int li = System.Array.IndexOf(_td.terrainLayers, layer);
            if (li < 0) continue;

            Vector3 wpos = sp.transform.position;
            Vector3 local = wpos - tp;
            int cx = Mathf.RoundToInt(local.x / size.x * (res - 1));
            int cz = Mathf.RoundToInt(local.z / size.z * (res - 1));

            float rad = Mathf.Max(0.01f, sp.bakeRadius * 1.5f);
            int rX = Mathf.CeilToInt(rad / cellX), rZ = Mathf.CeilToInt(rad / cellZ);

            int x0 = Mathf.Clamp(cx - rX, 0, res - 1), x1 = Mathf.Clamp(cx + rX, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rZ, 0, res - 1), z1 = Mathf.Clamp(cz + rZ, 0, res - 1);

            for (int z = z0; z <= z1; z++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) * cellX, dz = (z - cz) * cellZ;
                    float d2 = dx * dx + dz * dz;
                    if (d2 > rad * rad) continue;

                    float t = 1f - Mathf.Sqrt(d2) / rad;
                    float sum = 0f;
                    for (int l = 0; l < layers; l++)
                    {
                        float val = (l == li) ? Mathf.Lerp(alphas[z, x, l], 1f, t)
                                              : Mathf.Lerp(alphas[z, x, l], 0f, t);
                        alphas[z, x, l] = val;
                        sum += val;
                    }
                    for (int l = 0; l < layers; l++) alphas[z, x, l] /= sum;
                }
            }
        }

        _td.SetAlphamaps(0, 0, alphas);
    }

    // ---------- Despawn ----------
    private void SafeDespawn(GameObject go)
    {
        if (!go) return;

        // 1) 우선 비활성(풀과도 잘 맞음)
        if (preferDeactivateOverDestroy)
        {
            // 충돌/물리 정지
            if (go.TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            go.SetActive(false);
            return;
        }

        // 2) 아니면 그냥 파괴
        Destroy(go);
    }
}
