using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainDeformManager : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData;
    private int _hmResolution;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
        _hmResolution = _terrainData.heightmapResolution;
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
}
