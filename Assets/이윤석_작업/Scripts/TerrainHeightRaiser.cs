using UnityEngine;

public class TerrainHeightRaiser : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData, _originalData;
    private void Start()
    {
        _terrain = GetComponent<Terrain>();
        _originalData = _terrain.terrainData;
        _terrainData = _terrain.terrainData = Instantiate(_originalData);
        GetComponent<TerrainCollider>().terrainData = _terrainData;
    }

    public void RaiseHeight(Vector3 point, float brushRadius = 5f, float brushStrength = 0.0001f)
    {
        brushRadius *= 2;
        Vector3 terrainPos = point - _terrain.transform.position;
        int Res = _terrainData.heightmapResolution;

        int posX = (int)(terrainPos.x / _terrainData.size.x * Res);
        int posZ = (int)(terrainPos.z / _terrainData.size.z * Res);

        int rectWidth = Mathf.RoundToInt(brushRadius / _terrainData.size.x * Res);
        int rectHeight = Mathf.RoundToInt(brushRadius / _terrainData.size.z * Res);

        int startX = Mathf.Clamp(posX - rectWidth / 2, 0, Res);
        int startZ = Mathf.Clamp(posZ - rectHeight / 2, 0, Res);

        int width = Mathf.Clamp(rectWidth, 1, Res - startX);
        int height = Mathf.Clamp(rectHeight, 1, Res - startZ);

        float[,] heights = _terrainData.GetHeights(startX, startZ, width, height);

        for (int xIdx = 0; xIdx < width; xIdx++)
        {
            for (int yIdx = 0; yIdx < height; yIdx++)
            {
                //거리 기반 감쇠
                float dx = xIdx - width / 2f;
                float dz = yIdx - height / 2f;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float maxDist = Mathf.Sqrt((width / 2f) * (width / 2f) + (height / 2f) * (height / 2f));
                float falloff = 1 - (distance / maxDist);
                falloff = Mathf.Clamp01(falloff);

                float pileShape = heights[yIdx, xIdx] + falloff;//최종 쌓이는 정도

                heights[yIdx, xIdx] += pileShape * brushStrength;
            }
        }

        _terrainData.SetHeights(startX, startZ, heights);
    }


    private void OnDisable()
    {
        if (_originalData == null)
        {
            return;
        }

        _terrain.terrainData = _originalData;
        GetComponent<TerrainCollider>().terrainData = _originalData;
        _originalData = null;
    }
}