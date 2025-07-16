using UnityEngine;

/// <summary>
/// Initializes the Terrain at scene start based on settings:
/// - Flat baseline height
/// - Copy heightmap from another Terrain
/// - Reset painted textures to a single layer
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainInitializerMerged : MonoBehaviour
{
    [Header("Flat Baseline Settings")]
    [Tooltip("Enable to flatten the terrain to a uniform height.")]
    [SerializeField] private bool useFlatBaseline = true;
    [Tooltip("Normalized height (0-1) for flat baseline.")]
    [SerializeField, Range(0f, 1f)] private float baselineNorm = 0f;

    [Header("Source Copy Settings")]
    [Tooltip("Enable to copy heightmap data from another terrain.")]
    [SerializeField] private bool useSourceCopy = false;
    [Tooltip("Source Terrain to copy heightmap from.")]
    [SerializeField] private Terrain sourceTerrain;

    [Header("Texture Reset Settings")]
    [Tooltip("Enable to reset all painted textures to a single layer.")]
    [SerializeField] private bool resetTextures = false;
    [Tooltip("Index of the terrain layer to assign when resetting textures.")]
    [SerializeField] private int defaultTextureLayerIndex = 0;

    private Terrain _terrain;
    private TerrainData _terrainData;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
    }

    void Start()
    {
        if (useFlatBaseline)
            InitializeFlatHeight();

        if (useSourceCopy && sourceTerrain != null)
            CopySourceHeight();

        if (resetTextures)
            InitializeTextures();
    }

    /// <summary>
    /// Sets all heights to baselineNorm.
    /// </summary>
    private void InitializeFlatHeight()
    {
        int res = _terrainData.heightmapResolution;
        float[,] heights = new float[res, res];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                heights[z, x] = baselineNorm;
        _terrainData.SetHeights(0, 0, heights);
    }

    /// <summary>
    /// Copies heightmap from sourceTerrain, respecting resolution differences.
    /// </summary>
    private void CopySourceHeight()
    {
        TerrainData srcData = sourceTerrain.terrainData;
        int srcRes = srcData.heightmapResolution;
        int dstRes = _terrainData.heightmapResolution;
        int copyRes = Mathf.Min(srcRes, dstRes);

        float[,] srcHeights = srcData.GetHeights(0, 0, copyRes, copyRes);

        if (copyRes != dstRes)
        {
            float[,] fullHeights = new float[dstRes, dstRes];
            // Copy source
            for (int z = 0; z < copyRes; z++)
                for (int x = 0; x < copyRes; x++)
                    fullHeights[z, x] = srcHeights[z, x];
            // Fill rest with baseline
            for (int z = copyRes; z < dstRes; z++)
                for (int x = 0; x < dstRes; x++)
                    fullHeights[z, x] = baselineNorm;
            for (int z = 0; z < copyRes; z++)
                for (int x = copyRes; x < dstRes; x++)
                    fullHeights[z, x] = baselineNorm;

            _terrainData.SetHeights(0, 0, fullHeights);
        }
        else
        {
            _terrainData.SetHeights(0, 0, srcHeights);
        }
    }

    /// <summary>
    /// Resets all splatmaps (texture weights) so that only one layer is painted.
    /// </summary>
    private void InitializeTextures()
    {
        int alphaRes = _terrainData.alphamapResolution;
        int layerCount = _terrainData.terrainLayers.Length;

        // Create an empty splatmap array
        float[,,] alphas = new float[alphaRes, alphaRes, layerCount];

        // Assign full weight to the default layer
        for (int z = 0; z < alphaRes; z++)
        {
            for (int x = 0; x < alphaRes; x++)
            {
                for (int l = 0; l < layerCount; l++)
                {
                    alphas[z, x, l] = (l == defaultTextureLayerIndex) ? 1f : 0f;
                }
            }
        }

        _terrainData.SetAlphamaps(0, 0, alphas);
    }
}
