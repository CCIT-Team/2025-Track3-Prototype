using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 입자들이 쌓인 형태를 깊이(bakeRadius)를 가진 구 형태로 Terrain에 베이크하는 매니저.
/// SoilParticle에서 RegisterStop()으로 모은 입자를, autoBakeInterval이 지나면 자동으로 처리합니다.
/// </summary>
public class TerrainRaiseManager : MonoBehaviour
{
    [Header("Terrain 참조")]
    [SerializeField] private Terrain terrain;

    [Header("자동 Bake 설정")]
    public bool autoBakeEnabled = true;
    public float autoBakeInterval = 5f; // 초 단위
    private float _bakeTimer = 0f;

    private TerrainData _terrainData;
    private TerrainCollider _terrainCollider;
    private List<GameObject> _stopped = new List<GameObject>();

    void Awake()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;
        _terrainData = terrain.terrainData;
        _terrainCollider = terrain.GetComponent<TerrainCollider>();
    }

    /// <summary> SoilParticle에서 호출: 입자가 지형에 정착된 위치를 등록 </summary>
    public void RegisterStop(GameObject particle)
    {
        if (particle != null && !_stopped.Contains(particle))
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

    /// <summary>
    /// 등록된 입자들로부터 Terrain에 베이크하고, 입자 오브젝트를 제거합니다.
    /// Collider 비활성화와 Rigidbody 동결로 튀어오름을 방지합니다.
    /// </summary>
    private void BakeAndClearParticles()
    {
        if (_stopped.Count == 0) return;

        // 1) TerrainCollider 비활성화
        if (_terrainCollider != null)
            _terrainCollider.enabled = false;

        // 2) 입자 물리 정지: Collider 비활성, Rigidbody 제약만 설정
        foreach (var go in _stopped)
        {
            if (go == null) continue;
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 이미 kinematic 상태가 아니면 속도 초기화
                if (!rb.isKinematic)
                {
                    rb.velocity        = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                // 그 다음 kinematic 전환 및 완전 동결
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        // 3) Terrain 베이크
        BakeTerrainFromParticles();

        // 4) TerrainCollider 재활성화
        if (_terrainCollider != null)
            _terrainCollider.enabled = true;

        // 5) 입자 제거 및 리스트 초기화
        foreach (var go in _stopped)
            if (go != null) Destroy(go);
        _stopped.Clear();
    }

    /// <summary>
    /// 등록된 모든 입자를 순회하며, 입자 중심을 중심으로 구 형태(bakeRadius) 범위 내의 heightmap 셀을
    /// 입자 중심 높이로 맞춥니다.
    /// </summary>
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

        foreach (var go in _stopped)
        {
            if (go == null) continue;
            Vector3 worldPos = go.transform.position;

            var sp = go.GetComponent<SoilParticle>();
            float radius = sp != null ? sp.bakeRadius : 0f;
            float centerY = worldPos.y + (sp != null ? sp.heightOffset : 0f);
            float normTarget = Mathf.Clamp01((centerY - tPos.y) / mapSizeY);

            Vector3 localPos = worldPos - tPos;
            int cx = Mathf.RoundToInt(localPos.x / mapSizeX * (res - 1));
            int cz = Mathf.RoundToInt(localPos.z / mapSizeZ * (res - 1));

            int rX = Mathf.CeilToInt(radius / cellSizeX);
            int rZ = Mathf.CeilToInt(radius / cellSizeZ);

            int x0 = Mathf.Clamp(cx - rX, 0, res - 1);
            int x1 = Mathf.Clamp(cx + rX, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rZ, 0, res - 1);
            int z1 = Mathf.Clamp(cz + rZ, 0, res - 1);

            float rr = radius * radius;

            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) * cellSizeX;
                    float dz = (z - cz) * cellSizeZ;
                    if (dx * dx + dz * dz > rr) continue;

                    float cellY = heights[z, x] * mapSizeY + tPos.y;
                    float dy = worldPos.y - cellY;
                    if (dy * dy + dx * dx + dz * dz > rr) continue;

                    if (heights[z, x] < normTarget)
                        heights[z, x] = normTarget;
                }
        }

        _terrainData.SetHeights(0, 0, heights);
    }
}
