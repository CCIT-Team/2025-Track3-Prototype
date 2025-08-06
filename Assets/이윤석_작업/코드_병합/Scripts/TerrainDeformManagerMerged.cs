using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainDeformManagerMerged : MonoBehaviour
{
    [Header("Terrain Reference")]
    [SerializeField] private Terrain _terrain;
    [Header("Data Reference")]
    [SerializeField] private TerrainData _terrainData;
    [Header("Batch Settings")]
    [Tooltip("한 프레임에 처리할 최대 셀 개수")]
    [SerializeField] private int maxCellsPerBatch = 1024;

    private int _hmResolution;
    private int _amResolution;
    private float[,] _initialHeights;
    [SerializeField][Range(0,1f)] private float _diggedTextureMin = 0f;
    [SerializeField][Range(0,1f)] private float _diggedTextureMax = 1f;

    // 비동기 처리를 위한 누적값 및 코루틴 참조
    private Coroutine _digCoroutine;
    private float _pendingVolume;
    private float _pendingMaxDepth;
    private Vector3 _pendingMin = Vector3.one * float.MaxValue;
    private Vector3 _pendingMax = Vector3.one * float.MinValue;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
        _hmResolution = _terrainData.heightmapResolution;
        _amResolution = _terrainData.alphamapResolution;
        _initialHeights = _terrainData.GetHeights(0, 0, _hmResolution, _hmResolution);
    }

    /// <summary>
    /// 비동기 굴착 호출: 매 호출마다 입력값을 누적하고, 단일 코루틴으로 처리
    /// </summary>
    public void LowerRectAABBAsync(Vector3 min, Vector3 max, float targetVolume, float maxDepth)
    {
        // 값 누적
        _pendingVolume += targetVolume;
        _pendingMaxDepth = Mathf.Max(_pendingMaxDepth, maxDepth);
        _pendingMin = Vector3.Min(_pendingMin, min);
        _pendingMax = Vector3.Max(_pendingMax, max);

        // 코루틴 미실행 시에만 시작
        if (_digCoroutine == null)
            _digCoroutine = StartCoroutine(LowerRectBatchCoroutine());
    }

    private IEnumerator LowerRectBatchCoroutine()
    {
        // 누적값 로컬 복사 및 초기화
        var volume = _pendingVolume;
        var maxDepth = _pendingMaxDepth;
        var min = _pendingMin;
        var max = _pendingMax;
        _pendingVolume = 0f;
        _pendingMaxDepth = 0f;
        _pendingMin = Vector3.one * float.MaxValue;
        _pendingMax = Vector3.one * float.MinValue;

        // AABB 영역을 heightmap 좌표로 변환
        int x0 = Mathf.Clamp(Mathf.RoundToInt((min.x - _terrain.transform.position.x) / _terrainData.size.x * _hmResolution), 0, _hmResolution - 1);
        int z0 = Mathf.Clamp(Mathf.RoundToInt((min.z - _terrain.transform.position.z) / _terrainData.size.z * _hmResolution), 0, _hmResolution - 1);
        int x1 = Mathf.Clamp(Mathf.RoundToInt((max.x - _terrain.transform.position.x) / _terrainData.size.x * _hmResolution), 0, _hmResolution - 1);
        int z1 = Mathf.Clamp(Mathf.RoundToInt((max.z - _terrain.transform.position.z) / _terrainData.size.z * _hmResolution), 0, _hmResolution - 1);

        int w = Mathf.Abs(x1 - x0) + 1;
        int h = Mathf.Abs(z1 - z0) + 1;
        if (w <= 0 || h <= 0) { _digCoroutine = null; yield break; }

        float sizeY = _terrainData.size.y;
        float cellArea = (_terrainData.size.x / (_hmResolution - 1)) * (_terrainData.size.z / (_hmResolution - 1));
        float normDec = volume / (w * h * cellArea * sizeY);

        float[,] heights = _terrainData.GetHeights(x0, z0, w, h);
        float cx = w * 0.5f, cz = h * 0.5f;
        float invR2 = 1f / (Mathf.Max(cx, cz) * Mathf.Max(cx, cz));
        int total = w * h;
        int processed = 0;

        while (processed < total)
        {
            int batch = Mathf.Min(maxCellsPerBatch, total - processed);
            for (int k = 0; k < batch; k++)
            {
                int idx = processed + k;
                int ix = idx % w;
                int iz = idx / w;
                float dx = ix - cx;
                float dz = iz - cz;
                float falloff = Mathf.Clamp01(1f - (dx*dx + dz*dz) * invR2);

                float init = _initialHeights[z0 + iz, x0 + ix];
                float cur = heights[iz, ix];
                float already = init - cur;
                float allow = Mathf.Max(0f, (maxDepth * falloff) / sizeY - already);
                float dec = Mathf.Min(normDec, allow, cur);

                heights[iz, ix] = cur - dec;
            }
            processed += batch;
            _terrainData.SetHeights(x0, z0, heights);
            yield return null;
        }
        _digCoroutine = null;
    }

    /// <summary>
    /// 기존 페인트 로직 그대로 동기 호출
    /// </summary>
    public void PaintTexture(Vector3 min, Vector3 max, TerrainLayer targetLayer, float weight)
    {
        int layerIndex = -1;
        var layers = _terrainData.terrainLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == targetLayer) { layerIndex = i; break; }
        }
        if (layerIndex < 0) { Debug.LogWarning($"PaintTexture: 레이어 '{targetLayer.name}' 미발견"); return; }

        Vector3 tPos = _terrain.transform.position;
        int xStart = Mathf.Clamp(Mathf.RoundToInt((min.x - tPos.x) / _terrainData.size.x * _amResolution), 0, _amResolution - 1);
        int zStart = Mathf.Clamp(Mathf.RoundToInt((min.z - tPos.z) / _terrainData.size.z * _amResolution), 0, _amResolution - 1);
        int xEnd   = Mathf.Clamp(Mathf.RoundToInt((max.x - tPos.x) / _terrainData.size.x * _amResolution), 0, _amResolution - 1);
        int zEnd   = Mathf.Clamp(Mathf.RoundToInt((max.z - tPos.z) / _terrainData.size.z * _amResolution), 0, _amResolution - 1);

        int sizeX = Mathf.Abs(xEnd - xStart) + 1;
        int sizeZ = Mathf.Abs(zEnd - zStart) + 1;
        if (sizeX <= 0 || sizeZ <= 0) return;

        float[,,] alpha = _terrainData.GetAlphamaps(xStart, zStart, sizeX, sizeZ);
        int len = alpha.GetLength(2);
        for (int z = 0; z < sizeZ; z++)
        for (int x = 0; x < sizeX; x++)
        {
            float v = alpha[z, x, layerIndex];
            if (v <= _diggedTextureMin) alpha[z, x, layerIndex] = 0.2f;
            else if (v <= _diggedTextureMax) alpha[z, x, layerIndex] += weight;
            float sum = 0f;
            for (int i = 0; i < len; i++) sum += alpha[z, x, i];
            for (int i = 0; i < len; i++) alpha[z, x, i] /= sum;
        }
        _terrainData.SetAlphamaps(xStart, zStart, alpha);
    }
}
