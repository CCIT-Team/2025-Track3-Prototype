using UnityEngine;

/// <summary>
/// 씬 시작 시 Terrain을 평평하게 초기화하거나
/// 저장된 Heightmap 데이터를 직접 불러오는 컴포넌트
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainInitializer : MonoBehaviour
{
    [Header("초기화 옵션")]
    [Tooltip("씬 시작 시 지형을 이 높이로 평탄화합니다 (normalized 0~1)")]
    public float baselineNorm = 0;

    [Header("불러오기 옵션")]
    [Tooltip("시작 시 다른 Terrain의 Heightmap 데이터를 복사해 옵니다.")]
    public Terrain sourceTerrain;

    private Terrain     _terrain;
    private TerrainData _terrainData;

    void Awake()
    {
        _terrain     = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
    }

    void Start()
    {
        // 1) 평탄화
        int res = _terrainData.heightmapResolution;
        float[,] flatHeights = new float[res, res];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                flatHeights[z, x] = baselineNorm;
        _terrainData.SetHeights(0, 0, flatHeights);

        // 2) 소스 Terrain 데이터 복사
        if (sourceTerrain != null)
        {
            var srcData = sourceTerrain.terrainData;
            int srcRes  = srcData.heightmapResolution;
            int copyRes = Mathf.Min(res, srcRes);
            float[,] srcHeights = srcData.GetHeights(0, 0, copyRes, copyRes);

            _terrainData.heightmapResolution = copyRes;
            _terrainData.SetHeights(0, 0, srcHeights);
        }
    }
}
