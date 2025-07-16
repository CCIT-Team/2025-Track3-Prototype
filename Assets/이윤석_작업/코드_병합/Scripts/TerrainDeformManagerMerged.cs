using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainDeformManagerMerged : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData;
    private int _hmResolution;
    private int _amResolution;

    // 원본 높이맵을 저장해 두어, 누적 깊이 제한에 사용
    private float[,] _initialHeights;
    [SerializeField][Range(0,1)]
    private float _diggedTextureMin=0;
    [SerializeField][Range(0,1)]
    private float _diggedTextureMax=1;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
        _hmResolution = _terrainData.heightmapResolution;
        _amResolution = _terrainData.alphamapResolution;

        // 전체 높이맵(정규화된 0~1 값)을 보관
        _initialHeights = _terrainData.GetHeights(
            0, 0, _hmResolution, _hmResolution
        );
    }

    public void PaintTexture(Vector3 min, Vector3 max, TerrainLayer targetLayer, float weight)
    {
        int layerIndex = -1;

        for (int i = 0; i < _terrainData.terrainLayers.Length; i++)
        {
            if (_terrainData.terrainLayers[i] == targetLayer)
            {
                layerIndex = i;
                break;
            }
        }

        if (layerIndex < 0)
        {
            Debug.Log("찾을 수 없는 레이어");
            return;
        }

        Vector3 terrainPos = _terrain.transform.position;
        int xStart = Mathf.Clamp(
            Mathf.RoundToInt((min.x - terrainPos.x) / _terrainData.size.x * _amResolution),
            0, _amResolution - 1);
        int zStart = Mathf.Clamp(
            Mathf.RoundToInt((min.z - terrainPos.z) / _terrainData.size.z * _amResolution),
            0, _amResolution - 1);
        int xEnd = Mathf.Clamp(
            Mathf.RoundToInt((max.x - terrainPos.x) / _terrainData.size.x * _amResolution),
            0, _amResolution - 1);
        int zEnd = Mathf.Clamp(
            Mathf.RoundToInt((max.z - terrainPos.z) / _terrainData.size.z * _amResolution),
            0, _amResolution - 1);

        int sizeX = Mathf.Abs(xEnd - xStart) + 1;
        int sizeZ = Mathf.Abs(zEnd - zStart) + 1;
        if (sizeX <= 0 || sizeZ <= 0) return;

        float[,,] alphaMaps = _terrainData.GetAlphamaps(xStart, zStart, sizeX, sizeZ);
        int layerLength = alphaMaps.GetLength(2);
        

        for (int z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                if (alphaMaps[z, x, layerIndex] <= _diggedTextureMin)
                {
                    alphaMaps[z, x, layerIndex] = 0.2f;
                }
                else if (alphaMaps[z,x,layerIndex] <= _diggedTextureMax)
                {
                    alphaMaps[z, x, layerIndex] += weight;
                }
                
                //normalization
                float sum = 0;
                for (int i = 0; i < layerLength; i++)
                {
                    sum += alphaMaps[z, x, i];
                }

                for (int i = 0; i < layerLength; i++)
                {
                    alphaMaps[z, x, i]/=sum;
                }
            }
        }

        _terrainData.SetAlphamaps(xStart, zStart, alphaMaps);
    }

    /// <summary>
    /// Rectangular AABB 기준으로 terrain을 파내며, 
    /// 침투 깊이(maxDepth)를 넘어서는 파기를 방지합니다.
    /// (누적해서도 maxDepth 이상은 깎이지 않습니다.)
    /// 파낸 실제 부피(totalDeformedVol)를 m³ 단위로 반환합니다.
    /// </summary>
    public float LowerRectAABB(Vector3 min, Vector3 max, float targetVolume, float maxDepth)
    {
        Vector3 terrainPos = _terrain.transform.position;
        int xStart = Mathf.Clamp(
            Mathf.RoundToInt((min.x - terrainPos.x) / _terrainData.size.x * _hmResolution),
            0, _hmResolution - 1);
        int zStart = Mathf.Clamp(
            Mathf.RoundToInt((min.z - terrainPos.z) / _terrainData.size.z * _hmResolution),
            0, _hmResolution - 1);
        int xEnd   = Mathf.Clamp(
            Mathf.RoundToInt((max.x - terrainPos.x) / _terrainData.size.x * _hmResolution),
            0, _hmResolution - 1);
        int zEnd   = Mathf.Clamp(
            Mathf.RoundToInt((max.z - terrainPos.z) / _terrainData.size.z * _hmResolution),
            0, _hmResolution - 1);

        int sizeX = Mathf.Abs(xEnd - xStart) + 1;
        int sizeZ = Mathf.Abs(zEnd - zStart) + 1;
        if (sizeX <= 0 || sizeZ <= 0) return 0f;

        float mapSizeY = _terrainData.size.y;
        float[,] heights = _terrainData.GetHeights(xStart, zStart, sizeX, sizeZ);
        float totalDeformedVol = 0f;

        float cellSizeX = _terrainData.size.x / (_hmResolution - 1);
        float cellSizeZ = _terrainData.size.z / (_hmResolution - 1);
        float pixelArea = cellSizeX * cellSizeZ;

        // targetVolume m³ -> normalized height decrease per cell
        float heightNormDecrease = targetVolume 
                                  / (sizeX * sizeZ * pixelArea * mapSizeY);

        // AABB 안에서 중심/반경 계산 (셀 단위)
        float cx = sizeX * 0.5f;
        float cz = sizeZ * 0.5f;
        float radius = Mathf.Max(sizeX, sizeZ) * 0.5f;

        // 블록 내부 각 셀 순회
        for (int xi = 0; xi < sizeX; xi++)
        {
            for (int zi = 0; zi < sizeZ; zi++)
            {
                // 1) 거리 기반 falloff 계산 (0~1)
                float dx = xi - cx;
                float dz = zi - cz;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float falloff = Mathf.Clamp01(1f - dist / radius);

                // 2) 셀 절대 인덱스
                int absX = xStart + xi;
                int absZ = zStart + zi;

                // 3) 원본 높이 정규화값
                float initNorm = _initialHeights[absZ, absX];
                // 4) 현재 높이 정규화값
                float currentNorm = heights[zi, xi];

                // 5) 이미 깎인 비율 = init - current
                float alreadyDec = initNorm - currentNorm;

                // 6) 이 셀에 허용된 누적 최대 감소 비율
                float maxNormDepth = (maxDepth * falloff) / mapSizeY;
                float remainNorm = Mathf.Max(0f, maxNormDepth - alreadyDec);

                // 7) 한 프레임당 깎을 비율은
                //    heightNormDecrease (목표량) vs remainNorm vs currentNorm 중 최소
                float decreaseNorm = Mathf.Min(
                    heightNormDecrease,
                    remainNorm,
                    currentNorm
                );

                // 8) 적용
                heights[zi, xi] -= decreaseNorm;
                totalDeformedVol += decreaseNorm * mapSizeY * pixelArea;
            }
        }

        // 최종 반영
        _terrainData.SetHeights(xStart, zStart, heights);
        return totalDeformedVol;
    }




    public PixelTextureDataMerged[] SelectTextures(Vector3 min, Vector3 max, int maxSampleCnt)
    {
        Vector3 terrainPos = _terrain.transform.position;
        int xStart = Mathf.Clamp(
            Mathf.RoundToInt((min.x - terrainPos.x) / _terrainData.size.x * _amResolution),
            0, _amResolution - 1);
        int zStart = Mathf.Clamp(
            Mathf.RoundToInt((min.z - terrainPos.z) / _terrainData.size.z * _amResolution),
            0, _amResolution - 1);
        int xEnd = Mathf.Clamp(
            Mathf.RoundToInt((max.x - terrainPos.x) / _terrainData.size.x * _amResolution),
            0, _amResolution - 1);
        int zEnd = Mathf.Clamp(
            Mathf.RoundToInt((max.z - terrainPos.z) / _terrainData.size.z * _amResolution),
            0, _amResolution - 1);

        int sizeX = Mathf.Abs(xEnd - xStart) + 1;
        int sizeZ = Mathf.Abs(zEnd - zStart) + 1;
        if (sizeX <= 0 || sizeZ <= 0) return null;

        float[,,] alphamaps = _terrainData.GetAlphamaps(xStart, zStart, sizeX, sizeZ);
        PixelTextureDataMerged[] selected = new PixelTextureDataMerged[maxSampleCnt];

        int layerCnt = alphamaps.GetLength(2);

        for (int i = 0; i < maxSampleCnt; i++)
        {
            int x = UnityEngine.Random.Range(xStart, xEnd);
            int z = UnityEngine.Random.Range(zStart, zEnd);

            int hIndex = 0;
            int index = 0;
            float maxRate = alphamaps[z - zStart, x - xStart, 0];
            for (; index < layerCnt; index++)
            {
                if (alphamaps[z - zStart, x - xStart, index] > maxRate)
                {
                    maxRate = alphamaps[z - zStart, x - xStart, index];
                    hIndex = index;
                }
            }
            selected[i] = new PixelTextureDataMerged(_terrainData.terrainLayers[hIndex], x, z);

        }

        return selected;
    }
}
