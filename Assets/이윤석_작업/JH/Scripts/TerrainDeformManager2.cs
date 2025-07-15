using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainDeformManager2 : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData;
    private int _hmResolution;
    private int _amResolution;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
        _hmResolution = _terrainData.heightmapResolution;
        _amResolution = _terrainData.alphamapResolution;
    }

    /// <summary>
    /// Rectangular AABB 기준으로 terrain을 파내며, 침투 깊이(maxDepth)를 넘어서는 파기를 방지합니다.
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
        int xEnd = Mathf.Clamp(
            Mathf.RoundToInt((max.x - terrainPos.x) / _terrainData.size.x * _hmResolution),
            0, _hmResolution - 1);
        int zEnd = Mathf.Clamp(
            Mathf.RoundToInt((max.z - terrainPos.z) / _terrainData.size.z * _hmResolution),
            0, _hmResolution - 1);

        int sizeX = Mathf.Abs(xEnd - xStart) + 1;
        int sizeZ = Mathf.Abs(zEnd - zStart) + 1;
        if (sizeX <= 0 || sizeZ <= 0) return 0f;

        float mapSizeY = _terrainData.size.y;
        float[,] heights = _terrainData.GetHeights(xStart, zStart, sizeX, sizeZ);
        float[,] deltaHeights = new float[sizeZ, sizeX];

        float cellSizeX = _terrainData.size.x / (_hmResolution - 1);
        float cellSizeZ = _terrainData.size.z / (_hmResolution - 1);
        float pixelArea = cellSizeX * cellSizeZ;

        // targetVolume m³ -> normalized height decrease per cell
        float heightNormDecrease = targetVolume / (sizeX * sizeZ * pixelArea * mapSizeY);
        float totalDeformedVol = 0f;

        float cx = sizeX * 0.5f;
        float cz = sizeZ * 0.5f;
        float radius = Mathf.Max(sizeX, sizeZ) * 0.5f;

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                float dx = x - cx;
                float dz = z - cz;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float falloff = Mathf.Clamp01(1f - dist / radius);

                // clamp depth by blade penetration
                float maxNormDepth = (maxDepth * falloff) / mapSizeY;
                float currentNorm = heights[z, x];
                float decreaseNorm = Mathf.Min(heightNormDecrease, currentNorm, maxNormDepth);

                // apply deformation
                heights[z, x] -= decreaseNorm;
                deltaHeights[z, x] = decreaseNorm;

                // accumulate actual m³: decreaseNorm * mapSizeY * pixelArea
                totalDeformedVol += decreaseNorm * mapSizeY * pixelArea;
            }
        }

        _terrainData.SetHeights(xStart, zStart, heights);
        return totalDeformedVol;
    }




    public PixelTextureData[] SelectTextures(Vector3 min, Vector3 max, int maxSampleCnt)
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
        PixelTextureData[] selected = new PixelTextureData[maxSampleCnt];

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
            selected[i] = new PixelTextureData(_terrainData.terrainLayers[hIndex], x, z);

        }

        return selected;
    }
}
