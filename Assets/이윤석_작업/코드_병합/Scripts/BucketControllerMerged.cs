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
    [SerializeField] private GameObject soilPrefab;
    [SerializeField] private int maxParticlesPerFrame = 500;

    [Header("Depth & Layers")]
    [SerializeField] private float depthOffset = 0.1f;
    [SerializeField] private LayerMask terrainLayer;

    [Header("VFX Settings")]
    [SerializeField] private GameObject dustVFXPrefab;
    [SerializeField] private float vfxCooldown = 0.5f;

    [Header("References")]
    [SerializeField] private TerrainDeformManagerMerged deformManager;
    [SerializeField] private Terrain terrain;
    [SerializeField] private ExcavatorController_publicMerged excavatorController;
    private BucketGrabberMultiMerged _modeCtrl;

    private Rigidbody _rb;
    private Collider _col;
    private TerrainCollider _terrainCollider;
    private float _particleAccumulator;
    private float nextVfxTime = 0f;

    public bool isDigging { get; private set; }

    [SerializeField] private Material[] _mats;
    private List<Material> _buffer = new List<Material>();
    [SerializeField] private TerrainLayer _diggedLayerTexture;
    [SerializeField] private float _diggedLayerWeight = 1f;

    private GameObjectPool _pool;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _col = GetComponent<Collider>();

        deformManager = deformManager ?? FindObjectOfType<TerrainDeformManagerMerged>();
        if (deformManager != null && !deformManager.gameObject.activeInHierarchy)
        {
            Debug.Log("[BucketControllerMerged] Activating TerrainDeformManagerMerged GameObject.");
            deformManager.gameObject.SetActive(true);
        }
        if (deformManager == null)
        {
            Debug.LogError("[BucketControllerMerged] TerrainDeformManagerMerged not found");
            enabled = false;
            return;
        }

        terrain = terrain ?? deformManager.GetComponent<Terrain>();
        _terrainCollider = deformManager.GetComponent<TerrainCollider>();

        _modeCtrl = GetComponentInParent<BucketGrabberMultiMerged>();
        excavatorController = excavatorController ?? FindObjectOfType<ExcavatorController_publicMerged>();
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

        // penetration calculation
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

        // Terrain modification
        if (deformManager != null)
        {
            deformManager.LowerRectAABBAsync(bb.min, bb.max, deltaVol, penetration);
            deformManager.PaintTexture(bb.min, bb.max, _diggedLayerTexture, _diggedLayerWeight);
        }

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

        for (int i = 0; i < toSpawn; i++)
        {
            float x = Random.Range(bb.min.x, bb.max.x);
            float z = Random.Range(bb.min.z, bb.max.z);
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y + 0.5f;
            var go = Instantiate(soilPrefab, new Vector3(x, y, z), Quaternion.identity);
            // 파티클 페인트용 레이어 설정
            if (go.TryGetComponent<SoilParticleMerged>(out var p))
            {
                p.SetLayer(_diggedLayerTexture);
            }

            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.mass = 0.1f;
            }
            if (go.TryGetComponent<Renderer>(out var ren))
            {
                int idx = System.Array.IndexOf(terrain.terrainData.terrainLayers, _diggedLayerTexture);
                if (idx >= 0 && idx < _mats.Length)
                {
                    _buffer.Clear();
                    _buffer.Add(_mats[idx]);
                    ren.SetMaterials(_buffer);
                }
            }
        }
    }

    public void SetPoolInstance(GameObjectPool pool) => _pool = pool;
    /// <summary>
    /// 버킷 모드 갱신
    /// </summary>
    private void UpdateMode()
    {
        if (excavatorController == null || _modeCtrl == null) return;
        float angle = excavatorController.bucketAngle;
        BucketGrabberMultiMerged.Mode desired;

        // angle < 0 => Dump, 0-45 Dig, else Idle
        if (angle < 0f)
            desired = BucketGrabberMultiMerged.Mode.Dump;
        else if (angle >= 0f && angle < 45f)
            desired = BucketGrabberMultiMerged.Mode.Dig;
        else
            desired = BucketGrabberMultiMerged.Mode.Idle;

        if (_modeCtrl.CurrentMode != desired)
            _modeCtrl.SetMode(desired);
    }/// <summary>
    /// 파기 감지 및 지면 충돌 무시 처리
    /// </summary>
    private void DetectDig()
    {
        float bladeY = bladeCollider.bounds.min.y;
        float groundY = terrain.SampleHeight(bladeTransform.position) + terrain.transform.position.y;
        isDigging = (groundY - bladeY) > depthOffset;
        if (_col != null && _terrainCollider != null)
            Physics.IgnoreCollision(_col, _terrainCollider, isDigging);
    }
    public bool ComparePoolInstance(GameObjectPool pool) => _pool == pool;
}
