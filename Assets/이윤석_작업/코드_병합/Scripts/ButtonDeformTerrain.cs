using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ButtonDeformTerrain : MonoBehaviour
{
    [Header("Deform Settings")]
    [Tooltip("한 번 깎일 깊이 (m)")]
    [SerializeField] private float carveDepth = 0.5f;
    [Tooltip("키를 누르면 한 번 실행될 키")]
    [SerializeField] private KeyCode triggerKey = KeyCode.P;

    [Header("Particle Settings")]
    [Tooltip("깎인 부피당 생성할 입자 수")]
    [SerializeField] private float particlePerCubicMeter = 10f;
    [Tooltip("토양 입자 프리팹 (SoilParticleMerged 포함)")]
    [SerializeField] private GameObject soilParticlePrefab;

    [Header("References")]
    [SerializeField] private TerrainDeformManagerMerged deformManager;
    [SerializeField] private Terrain terrain;

    private BoxCollider boxCol;

    void Awake()
    {
        boxCol = GetComponent<BoxCollider>();
        if (!boxCol.isTrigger)
            boxCol.isTrigger = true;

        if (deformManager == null)
            deformManager = FindObjectOfType<TerrainDeformManagerMerged>();
        if (terrain == null)
            terrain = Terrain.activeTerrain;
    }

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
            DoDeform();
    }

    private void DoDeform()
    {
        // 1) 영역 Bounds 계산
        Bounds bb = boxCol.bounds;
        float width = bb.size.x;
        float length = bb.size.z;
        float height = carveDepth;
        float volume = width * length * height;

        // 2) 지형 깎기 (Async 호출이라 즉시 높이는 안 바뀔 수 있음)
        // LowerRectAABBAsync(min, max, volume, penetration)
        // 여기선 penetration = carveDepth 으로 간주
        deformManager.LowerRectAABBAsync(bb.min, bb.max + Vector3.up * height, volume, carveDepth);

        // 3) SoilParticle 생성
        int toSpawn = Mathf.CeilToInt(volume * particlePerCubicMeter);
        for (int i = 0; i < toSpawn; i++)
        {
            // 랜덤한 x,z, y는 상자 위면 바로 위
            float x = Random.Range(bb.min.x, bb.max.x);
            float z = Random.Range(bb.min.z, bb.max.z);
            float y = bb.max.y + 0.1f;
            Instantiate(soilParticlePrefab, new Vector3(x, y, z), Random.rotation);
        }
    }
}
