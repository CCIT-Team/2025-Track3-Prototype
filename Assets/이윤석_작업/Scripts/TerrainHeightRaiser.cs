using System.Data.Common;
using System.Net.Mime;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class TerrainHeightRaiser : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData, _originalData;
    private int _terrainResolution;
    private int _alphamapResolution;
    private void Start()
    {
        _terrain = GetComponent<Terrain>();
        _originalData = _terrain.terrainData;
        _terrainData = _terrain.terrainData = Instantiate(_originalData);
        _terrainResolution = _terrainData.heightmapResolution;
        _alphamapResolution = _terrainData.alphamapResolution;
        GetComponent<TerrainCollider>().terrainData = _terrainData;
    }

    private void CalculatePos(out PiledTerrainDataPack outDataPack, Vector3 point, float brushRadius, int resolution)
    {
        brushRadius *= 2;
        Vector3 terrainPos = point - _terrain.transform.position;

        int posX = (int)(terrainPos.x / _terrainData.size.x * resolution);//Mathf.FloorToInt
        int posZ = (int)(terrainPos.z / _terrainData.size.z * resolution);

        int rectWidth = (int)(brushRadius / _terrainData.size.x * resolution); //Mathf.FloorToInt
        int rectHeight = (int)(brushRadius / _terrainData.size.z * resolution);

        outDataPack.startPosX = Mathf.Clamp(posX - rectWidth / 2, 0, resolution);
        outDataPack.startPosZ = Mathf.Clamp(posZ - rectHeight / 2, 0, resolution);

        outDataPack.width = Mathf.Clamp(rectWidth, 1, resolution - outDataPack.startPosX);
        outDataPack.height = Mathf.Clamp(rectHeight, 1, resolution - outDataPack.startPosZ);
        outDataPack.widthHalf = outDataPack.width * 0.5f;
        outDataPack.heightHalf = outDataPack.height * 0.5f;
        outDataPack.maxDistanceSqr = (outDataPack.widthHalf * outDataPack.widthHalf + outDataPack.heightHalf * outDataPack.heightHalf) * 0.5f; // == /2
    }

    public void PileTerrain(Vector3 point, TerrainLayer layer, float brushRadius = 5f, float brushStrength = 0.0001f)
    {
        PiledTerrainDataPack heightDataPack, alphaDataPack;
        CalculatePos(out heightDataPack, point, brushRadius, _terrainResolution);
        CalculatePos(out alphaDataPack, point, brushRadius, _alphamapResolution);

        RaiseHeight(in heightDataPack, brushStrength);
        SetTexture(in alphaDataPack, layer);
    }

    private void RaiseHeight(in PiledTerrainDataPack inDataPack, float brushStrength)
    {

        float[,] heights = _terrainData.GetHeights(inDataPack.startPosX, inDataPack.startPosZ, inDataPack.width, inDataPack.height);

        for (int xIdx = 0; xIdx < inDataPack.width; xIdx++)
        {
            for (int yIdx = 0; yIdx < inDataPack.height; yIdx++)
            {
                //거리 기반 감쇠
                float distX = xIdx - inDataPack.widthHalf;
                float distZ = yIdx - inDataPack.heightHalf;
                float falloff = Mathf.Clamp01(1 - Mathf.Sqrt((distX * distX + distZ * distZ) / inDataPack.maxDistanceSqr));// distance/maxDistance
                heights[yIdx, xIdx] += falloff * brushStrength;//최종적으로 쌓일 양에 brushStrength곱해서 조정
            }
        }
        _terrainData.SetHeights(inDataPack.startPosX, inDataPack.startPosZ, heights);
    }

    private void SetTexture(in PiledTerrainDataPack inDataPack, TerrainLayer targetLayer)
    {
        int layerIndex = -1;

        for (int i = 0; i < _terrainData.terrainLayers.Length; i++)
        {
            if (_terrainData.terrainLayers[i] == targetLayer)
            {
                layerIndex = i;
                Debug.Log(layerIndex);
                break;
            }
        }

        if (layerIndex < 0)
        {
            Debug.Log("찾을 수 없는 레이어");
            return;
        }

        float[,,] alphaMaps = _terrainData.GetAlphamaps(inDataPack.startPosX, inDataPack.startPosZ, inDataPack.width, inDataPack.height);
        int layerLength = alphaMaps.GetLength(2);

        for (int xIdx = 0; xIdx < inDataPack.width; xIdx++)
        {
            for (int yIdx = 0; yIdx < inDataPack.height; yIdx++)
            {
                float distX = xIdx - inDataPack.widthHalf;
                float distZ = yIdx - inDataPack.heightHalf;
                float rate = Mathf.Clamp01(1 - Mathf.Sqrt((distX * distX + distZ * distZ) / inDataPack.maxDistanceSqr));// distance/maxDistance
                alphaMaps[yIdx, xIdx, layerIndex] += rate;
                //normalization
                float sum = 0;
                for (int i = 0; i < layerLength; i++)
                {
                    sum += alphaMaps[yIdx, xIdx, i];
                }

                for (int i = 0; i < layerLength; i++)
                {
                    alphaMaps[yIdx, xIdx, i]/=sum;
                }
            }
        }
        _terrainData.SetAlphamaps(inDataPack.startPosX, inDataPack.startPosZ, alphaMaps);

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