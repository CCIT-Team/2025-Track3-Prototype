using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BucketControllerMerged : MonoBehaviour, IPoolable
{
    [Header("Blade Settings")]
    [SerializeField] private Collider bladeCollider;
    [SerializeField] private Transform bladeTransform;

    [Header("Excavation Settings")]
    [SerializeField] private float excavateRate = 3f;
    [SerializeField] private float particlePerCubicM = 30f;

    [Header("Spawn Settings")]
    [SerializeField, Tooltip("레이어별로 사용할 soil particle 프리팹을 지정하세요")]
    private GameObject[] soilPrefabs;
    [SerializeField] private int maxParticlesPerFrame = 500;

    [Header("Depth & Layers")]
    [SerializeField] private float depthOffset = 0.1f;
    [SerializeField] private LayerMask terrainLayer;

    [Header("VFX Settings")]
    [SerializeField] private GameObject dustVFXPrefab;
    [SerializeField] private float vfxCooldown = 0.5f;

    [Header("References (Assign in Inspector)")]
    [SerializeField, Tooltip("하나라도 할당하세요")]
    private TerrainDeformManagerMerged deformManager;
    [SerializeField, Tooltip("하나라도 할당하세요")]
    private Terrain terrain;
    [SerializeField, Tooltip("PublicMerged 버전")]
    private ExcavatorController_publicMerged excavatorControllerPublic;
    [SerializeField, Tooltip("Poly 버전")]
    private ExcavatorController_Poly excavatorControllerPoly;

    private BucketGrabberMultiMerged _modeCtrl;
    private Rigidbody _rb;
    private Collider _col;
    private TerrainCollider _terrainCollider;
    private float _particleAccumulator;
    private float nextVfxTime = 0f;

    public bool isDigging { get; private set; }

    [Header("Soil Particle Painting")]
    [SerializeField] private Material[] _mats;
    [SerializeField] private TerrainLayer _diggedLayerTexture;
    [SerializeField] private float _diggedLayerWeight = 1f;
    private List<Material> _buffer = new List<Material>();

    private GameObjectPool _pool;

    // 어느 컨트롤러로 동작할지
    private enum ControllerType { None, PublicMerged, Poly }
    private ControllerType _activeController = ControllerType.None;

    void Start()
    {
        // Rigidbody 세팅
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Collider 세팅
        _col = GetComponent<Collider>();

        // deformManager 체크
        if (deformManager == null)
        {
            Debug.LogError("[BucketControllerMerged] TerrainDeformManagerMerged 미할당!");
            enabled = false; return;
        }
        if (!deformManager.gameObject.activeInHierarchy)
            deformManager.gameObject.SetActive(true);

        // terrain 체크
        if (terrain == null)
        {
            Debug.LogError("[BucketControllerMerged] Terrain 미할당!");
            enabled = false; return;
        }
        _terrainCollider = deformManager.GetComponent<TerrainCollider>();

        // ExcavatorController 결정
        if (excavatorControllerPublic != null)
        {
            _activeController = ControllerType.PublicMerged;
        }
        else if (excavatorControllerPoly != null)
        {
            _activeController = ControllerType.Poly;
        }
        else
        {
            Debug.LogError("[BucketControllerMerged] 둘 중 하나의 ExcavatorController를 할당하세요!");
            enabled = false; return;
        }

        // Grabber 모드 컨트롤러
        _modeCtrl = GetComponentInParent<BucketGrabberMultiMerged>();
    }

    void FixedUpdate()
    {
        UpdateMode();
        DetectDig();

        if (!isDigging || _modeCtrl.CurrentMode != BucketGrabberMultiMerged.Mode.Dig)
            return;

        Bounds bb = bladeCollider.bounds;
        if (Physics.OverlapBox(bb.center, bb.extents, bladeCollider.transform.rotation, terrainLayer).Length == 0)
            return;

        float deltaVol = excavateRate * Time.fixedDeltaTime;

        // 땅 파기
        Vector3 tPos = terrain.transform.position;
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(bb.min.x, bb.min.y, bb.min.z),
            new Vector3(bb.min.x, bb.min.y, bb.max.z),
            new Vector3(bb.max.x, bb.min.y, bb.min.z),
            new Vector3(bb.max.x, bb.min.y, bb.max.z)
        };
        float penetration = 0f;
        foreach (var c in corners)
        {
            float groundY = terrain.SampleHeight(c) + tPos.y;
            penetration = Mathf.Max(penetration, groundY - c.y);
        }
        penetration = Mathf.Max(0f, penetration);

        deformManager.LowerRectAABBAsync(bb.min, bb.max, deltaVol, penetration);
        deformManager.PaintTexture(bb.min, bb.max, _diggedLayerTexture, _diggedLayerWeight);

        // Dust VFX
        if (dustVFXPrefab != null && Time.time >= nextVfxTime)
        {
            nextVfxTime = Time.time + vfxCooldown;
            Instantiate(dustVFXPrefab, bb.center, Quaternion.identity);
        }

        SpawnParticles(deltaVol, bb);
    }

    private void SpawnParticles(float carvedVol, Bounds bb)
    {
        if (carvedVol <= 0f || particlePerCubicM <= 0f) return;

        _particleAccumulator += carvedVol * particlePerCubicM;
        int toSpawn = Mathf.FloorToInt(_particleAccumulator);
        _particleAccumulator -= toSpawn;
        toSpawn = Mathf.Min(toSpawn, maxParticlesPerFrame);

        TerrainData tData = terrain.terrainData;
        Vector3 tPos = terrain.transform.position;
        int w = tData.alphamapWidth, h = tData.alphamapHeight;

        for (int i = 0; i < toSpawn; i++)
        {
            // 1) 랜덤한 월드 좌표
            float x = Random.Range(bb.min.x, bb.max.x);
            float z = Random.Range(bb.min.z, bb.max.z);

            // 2) 알파맵 좌표로 매핑
            int mapX = Mathf.Clamp((int)((x - tPos.x) / tData.size.x * w), 0, w - 1);
            int mapZ = Mathf.Clamp((int)((z - tPos.z) / tData.size.z * h), 0, h - 1);

            // 3) dominant layer 계산
            float[,,] alphas = tData.GetAlphamaps(mapX, mapZ, 1, 1);
            int dominantLayer = 0;
            float maxMix = 0f;
            for (int layer = 0; layer < alphas.GetLength(2); layer++)
            {
                if (alphas[0, 0, layer] > maxMix)
                {
                    maxMix = alphas[0, 0, layer];
                    dominantLayer = layer;
                }
            }

            // 4) 사용할 프리팹 선택 (배열 범위 체크)
            int idx = Mathf.Clamp(dominantLayer, 0, soilPrefabs.Length - 1);
            GameObject prefab = soilPrefabs[idx];

            // 5) 생성
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + tPos.y + 0.5f;
            var go = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity);

            // 6) (Optional) SoilParticleMerged에 레이어 정보 전달
            if (go.TryGetComponent<SoilParticleMerged>(out var p))
                p.SetLayer(terrain.terrainData.terrainLayers[dominantLayer]);

            // 7) 물리 설정
            if (go.TryGetComponent<Rigidbody>(out var rb))
                rb.mass = 0.1f;
        }
    }


    private void UpdateMode()
    {
        // 할당된 컨트롤러에서 bucketAngle 가져오기
        float angle = 0f;
        switch (_activeController)
        {
            case ControllerType.PublicMerged:
                angle = excavatorControllerPublic.bucketAngle;
                break;
            case ControllerType.Poly:
                angle = excavatorControllerPoly.bucketAngle;
                break;
        }

        // 모드 결정
        BucketGrabberMultiMerged.Mode desired;
        if (angle < 0f) desired = BucketGrabberMultiMerged.Mode.Dump;
        else if (angle < 45f) desired = BucketGrabberMultiMerged.Mode.Dig;
        else desired = BucketGrabberMultiMerged.Mode.Idle;

        if (_modeCtrl.CurrentMode != desired)
            _modeCtrl.SetMode(desired);
    }

    private void DetectDig()
    {
        float bladeY = bladeCollider.bounds.min.y;
        float groundY = terrain.SampleHeight(bladeTransform.position) + terrain.transform.position.y;
        isDigging = (groundY - bladeY) > depthOffset;

        if (_col != null && _terrainCollider != null)
            Physics.IgnoreCollision(_col, _terrainCollider, isDigging);
    }

    // IPoolable
    public void SetPoolInstance(GameObjectPool pool) => _pool = pool;
    public bool ComparePoolInstance(GameObjectPool pool) => _pool == pool;
}