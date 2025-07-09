using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance { get; private set; }

    [Header("핵심 설정")]
    public Terrain TargetTerrain;
    public int DirtTextureLayerIndex = 1;
    public GameObject SoilParticlePrefab;

    [Header("부피 및 입자 설정")]
    [Tooltip("흙 입자 하나가 대표하는 흙의 부피 (m^3)")]
    public float VolumePerParticle = 0.01f;
    [Tooltip("한 번에 생성될 수 있는 최대 입자 수")]
    public int MaxParticlesPerDig = 50;

    public Vector3 TerrainSize { get; private set; }
    private TerrainData terrainData;
    private int heightmapWidth;
    private int heightmapHeight;
    private float pixelArea;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (TargetTerrain == null) TargetTerrain = FindObjectOfType<Terrain>();
        if (TargetTerrain == null)
        {
            Debug.LogError("씬에 터레인이 없습니다!");
            this.enabled = false;
            return;
        }

        terrainData = TargetTerrain.terrainData;
        heightmapWidth = terrainData.heightmapResolution;
        heightmapHeight = terrainData.heightmapResolution;
        TerrainSize = terrainData.size;

        float pixelSizeX = TerrainSize.x / (heightmapWidth - 1);
        float pixelSizeZ = TerrainSize.z / (heightmapHeight - 1);
        pixelArea = pixelSizeX * pixelSizeZ;
    }

    public void Dig(Vector3 worldPos, float radius, float strength)
    {
        float dugVolume = ModifyTerrainHeight(worldPos, radius, -strength);

        if (dugVolume > 0)
        {
            int particleCount = (int)(dugVolume / VolumePerParticle);
            particleCount = Mathf.Min(particleCount, MaxParticlesPerDig);

            SpawnParticles(worldPos, particleCount);
            PaintTerrain(worldPos, radius);
        }
    }

    public void Pile(Vector3 worldPos, float radius, float strength)
    {
        ModifyTerrainHeight(worldPos, radius, strength);
    }

    private float ModifyTerrainHeight(Vector3 worldPos, float radius, float amount)
    {
        int pixelRadius = (int)(radius / (TerrainSize.x / (heightmapWidth - 1)));
        if (pixelRadius == 0) pixelRadius = 1;

        Vector3Int centerCoord = WorldToTerrainCoord(worldPos);

        int startX = Mathf.Max(0, centerCoord.x - pixelRadius);
        int endX = Mathf.Min(heightmapWidth, centerCoord.x + pixelRadius);
        int startY = Mathf.Max(0, centerCoord.z - pixelRadius);
        int endY = Mathf.Min(heightmapHeight, centerCoord.z + pixelRadius);

        float[,] heights = terrainData.GetHeights(startX, startY, endX - startX, endY - startY);
        float totalHeightChange = 0;

        for (int y = 0; y < endY - startY; y++)
        {
            for (int x = 0; x < endX - startX; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(pixelRadius, pixelRadius));
                if (distance < pixelRadius)
                {
                    float influence = 1 - (distance / pixelRadius);
                    float modification = amount * influence;

                    float originalHeight = heights[y, x];
                    heights[y, x] = Mathf.Clamp01(originalHeight + modification);
                    totalHeightChange += Mathf.Abs(heights[y, x] - originalHeight);
                }
            }
        }
        terrainData.SetHeights(startX, startY, heights);

        return totalHeightChange * pixelArea * TerrainSize.y;
    }

    private void PaintTerrain(Vector3 worldPos, float radius)
    {
        int pixelRadius = (int)(radius / (TerrainSize.x / (heightmapWidth - 1)));
        Vector3Int centerCoord = WorldToTerrainCoord(worldPos);
        Vector2Int alphamapCenter = new Vector2Int(
            (int)(centerCoord.x / (float)heightmapWidth * terrainData.alphamapWidth),
            (int)(centerCoord.z / (float)heightmapHeight * terrainData.alphamapHeight)
        );
        int alphamapRadius = (int)(pixelRadius / (float)heightmapWidth * terrainData.alphamapWidth);

        int startX = Mathf.Max(0, alphamapCenter.x - alphamapRadius);
        int endX = Mathf.Min(terrainData.alphamapWidth, alphamapCenter.x + alphamapRadius);
        int startY = Mathf.Max(0, alphamapCenter.y - alphamapRadius);
        int endY = Mathf.Min(terrainData.alphamapHeight, alphamapCenter.y + alphamapRadius);

        float[,,] alphamaps = terrainData.GetAlphamaps(startX, startY, endX - startX, endY - startY);
        int textureLayerCount = alphamaps.GetLength(2);

        for (int y = 0; y < endY - startY; y++)
        {
            for (int x = 0; x < endX - startX; x++)
            {
                for (int i = 0; i < textureLayerCount; i++)
                {
                    alphamaps[y, x, i] = (i == DirtTextureLayerIndex) ? 1f : 0f;
                }
            }
        }
        terrainData.SetAlphamaps(startX, startY, alphamaps);
    }

    private void SpawnParticles(Vector3 worldPos, int count)
    {
        if (SoilParticlePrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnOffset = Random.insideUnitSphere * 0.2f;
            Vector3 spawnPos = worldPos + Vector3.up * 0.1f + spawnOffset;
            Instantiate(SoilParticlePrefab, spawnPos, Random.rotation);
        }
    }

    private Vector3Int WorldToTerrainCoord(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - TargetTerrain.transform.position;
        int x = (int)((relativePos.x / TerrainSize.x) * heightmapWidth);
        int z = (int)((relativePos.z / TerrainSize.z) * heightmapHeight);
        return new Vector3Int(x, 0, z);
    }
}