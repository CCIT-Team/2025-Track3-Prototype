using UnityEngine;

/// <summary>
/// Paints a specified texture layer onto the terrain at a given world position and radius.
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainTexturePainter : MonoBehaviour
{
    private Terrain _terrain;
    private TerrainData _terrainData;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
    }

    /// <summary>
    /// Paints a single texture layer (textureIndex) at world position within the given radius.
    /// </summary>
    public void PaintAt(Vector3 worldPos, int textureIndex, float radius)
    {
        Vector3 terrainPos = _terrain.transform.position;
        int mapWidth = _terrainData.alphamapWidth;
        int mapHeight = _terrainData.alphamapHeight;
        int numLayers = _terrainData.alphamapLayers;

        // Convert world position to alphamap coordinates
        int x = Mathf.RoundToInt(((worldPos.x - terrainPos.x) / _terrainData.size.x) * mapWidth);
        int z = Mathf.RoundToInt(((worldPos.z - terrainPos.z) / _terrainData.size.z) * mapHeight);
        int radiusPx = Mathf.RoundToInt((radius / _terrainData.size.x) * mapWidth);

        int x0 = Mathf.Clamp(x - radiusPx, 0, mapWidth - 1);
        int x1 = Mathf.Clamp(x + radiusPx, 0, mapWidth - 1);
        int z0 = Mathf.Clamp(z - radiusPx, 0, mapHeight - 1);
        int z1 = Mathf.Clamp(z + radiusPx, 0, mapHeight - 1);

        float[,,] alphas = _terrainData.GetAlphamaps(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
        for (int i = 0; i < x1 - x0 + 1; i++)
        {
            for (int j = 0; j < z1 - z0 + 1; j++)
            {
                for (int l = 0; l < numLayers; l++)
                {
                    alphas[j, i, l] = (l == textureIndex) ? 1f : 0f;
                }
            }
        }
        _terrainData.SetAlphamaps(x0, z0, alphas);
    }
}
