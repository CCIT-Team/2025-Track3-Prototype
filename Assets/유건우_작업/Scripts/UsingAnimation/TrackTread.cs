using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackTread : MonoBehaviour
{
    public RenderTexture trackMask;
    public Material drawMaterial; // Brush Shader
    public Transform[] wheelPositions;

    public Terrain terrain;
    private Vector3 terrainOrigin;
    private Vector3 terrainSize;

    void Start()
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return;
        }

        terrainOrigin = terrain.GetPosition();
        terrainSize = terrain.terrainData.size;
    }

    void Update()
    {
        foreach (var wheel in wheelPositions)
        {
            Vector2 uv = WorldToUV(wheel.position);
            drawMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));

            // ✅ Double buffering으로 temp RenderTexture 사용
            RenderTexture temp = RenderTexture.GetTemporary(trackMask.width, trackMask.height, 0, trackMask.format);
            Graphics.Blit(trackMask, temp); // 먼저 기존 내용 복사

            Graphics.Blit(temp, trackMask, drawMaterial); // 새 자국 그리기

            RenderTexture.ReleaseTemporary(temp);
        }
    }

    Vector2 WorldToUV(Vector3 worldPos)
    {
        float u = (worldPos.x - terrainOrigin.x) / terrainSize.x;
        float v = (worldPos.z - terrainOrigin.z) / terrainSize.z;
        return new Vector2(u, v);
    }
}