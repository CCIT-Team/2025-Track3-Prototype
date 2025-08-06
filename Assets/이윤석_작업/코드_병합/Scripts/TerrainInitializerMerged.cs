using UnityEngine;

/// <summary>
/// Initializes the Terrain at scene start based on settings:
/// - Flat baseline height
/// - Copy heightmap from another Terrain
/// - Reset painted textures to a single layer
/// - Applies a high-friction PhysicMaterial to the TerrainCollider
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

    [Header("Terrain Friction Settings")]
    [Tooltip("Static friction for terrain collider (>=0)")]
    [SerializeField, Min(0f)] private float terrainStaticFriction = 1f;
    [Tooltip("Dynamic friction for terrain collider (>=0)")]
    [SerializeField, Min(0f)] private float terrainDynamicFriction = 1f;
    private TerrainData _terrainData;

    void Awake()
    {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;

        // Assign high-friction PhysicMaterial to TerrainCollider
        var tCol = _terrain.GetComponent<TerrainCollider>();
        if (tCol != null)
        {
            var frictionMat = new PhysicMaterial("TerrainFrictionMat")
            {
                staticFriction = terrainStaticFriction,
                dynamicFriction = terrainDynamicFriction,
                frictionCombine = PhysicMaterialCombine.Maximum,
                bounciness = 0f,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
            tCol.material = frictionMat;
        }
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

    private void InitializeFlatHeight()
    {
        int res = _terrainData.heightmapResolution;
        float[,] heights = new float[res, res];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                heights[z, x] = baselineNorm;
        _terrainData.SetHeights(0, 0, heights);
    }

    private void CopySourceHeight()
    {
        TerrainData src = sourceTerrain.terrainData;
        int srcRes = src.heightmapResolution;
        int dstRes = _terrainData.heightmapResolution;
        int copyRes = Mathf.Min(srcRes, dstRes);

        float[,] srcH = src.GetHeights(0, 0, copyRes, copyRes);
        if (copyRes != dstRes)
        {
            float[,] full = new float[dstRes, dstRes];
            for (int z = 0; z < copyRes; z++)
                for (int x = 0; x < copyRes; x++)
                    full[z, x] = srcH[z, x];
            for (int z = copyRes; z < dstRes; z++)
                for (int x = 0; x < dstRes; x++)
                    full[z, x] = baselineNorm;
            for (int z = 0; z < copyRes; z++)
                for (int x = copyRes; x < dstRes; x++)
                    full[z, x] = baselineNorm;
            _terrainData.SetHeights(0, 0, full);
        }
        else
        {
            _terrainData.SetHeights(0, 0, srcH);
        }
    }

    private void InitializeTextures()
    {
        int alphaRes = _terrainData.alphamapResolution;
        int layerCount = _terrainData.terrainLayers.Length;
        float[,,] alphas = new float[alphaRes, alphaRes, layerCount];

        for (int z = 0; z < alphaRes; z++)
            for (int x = 0; x < alphaRes; x++)
                for (int l = 0; l < layerCount; l++)
                    alphas[z, x, l] = (l == defaultTextureLayerIndex) ? 1f : 0f;

        _terrainData.SetAlphamaps(0, 0, alphas);
    }
}
