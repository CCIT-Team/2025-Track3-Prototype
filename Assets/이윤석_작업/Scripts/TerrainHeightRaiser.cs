using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class TerrainHeightRaiser : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData, _originalData;
    private int _terrainResolution;
    private void Start()
    {
        _terrain = GetComponent<Terrain>();
        _originalData = _terrain.terrainData;
        _terrainData = _terrain.terrainData = Instantiate(_originalData);
        _terrainResolution = _terrainData.heightmapResolution;
        GetComponent<TerrainCollider>().terrainData = _terrainData;
    }

    public void RaiseHeight(Vector3 point, float brushRadius = 5f, float brushStrength = 0.0001f)
    {
        brushRadius *= 2;
        Vector3 terrainPos = point - _terrain.transform.position;

        int posX = (int)(terrainPos.x / _terrainData.size.x * _terrainResolution);//Mathf.FloorToInt
        int posZ = (int)(terrainPos.z / _terrainData.size.z * _terrainResolution);

        int rectWidth = (int)(brushRadius / _terrainData.size.x * _terrainResolution); //Mathf.FloorToInt
        int rectHeight = (int)(brushRadius / _terrainData.size.z * _terrainResolution);

        int startX = Mathf.Clamp(posX - rectWidth / 2, 0, _terrainResolution);
        int startZ = Mathf.Clamp(posZ - rectHeight / 2, 0, _terrainResolution);

        int width = Mathf.Clamp(rectWidth, 1, _terrainResolution - startX);
        int height = Mathf.Clamp(rectHeight, 1, _terrainResolution - startZ);
        float widthHalf = width * 0.5f;
        float heightHalf = height * 0.5f;
        float maxDistanceSqr = (widthHalf * widthHalf + heightHalf * heightHalf) * 0.5f; // == /2

        float[,] heights = _terrainData.GetHeights(startX, startZ, width, height);

        for (int xIdx = 0; xIdx < width; xIdx++)
        {
            for (int yIdx = 0; yIdx < height; yIdx++)
            {
                //거리 기반 감쇠
                float distX = xIdx - widthHalf;
                float distZ = yIdx - heightHalf;
                float falloff = Mathf.Clamp01(1 - Mathf.Sqrt((distX * distX + distZ * distZ) / maxDistanceSqr));// distance/maxDistance
                heights[yIdx, xIdx] += falloff * brushStrength;//최종적으로 쌓일 양에 brushStrength곱해서 조정
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