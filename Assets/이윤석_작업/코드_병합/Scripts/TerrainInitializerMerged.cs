using UnityEngine;

/// <summary>
/// Initializes the Terrain at scene start based on settings:
/// - Flat baseline height
/// - Copy heightmap from another Terrain
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
            // Create a new full-size array and populate with source + baseline
            float[,] fullHeights = new float[dstRes, dstRes];
            for (int z = 0; z < copyRes; z++)
                for (int x = 0; x < copyRes; x++)
                    fullHeights[z, x] = srcHeights[z, x];
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
}
