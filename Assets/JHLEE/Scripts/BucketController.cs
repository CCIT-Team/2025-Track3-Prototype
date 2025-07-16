using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BucketController : MonoBehaviour
{
    [Header("Blade Settings")]
    [Tooltip("Collider used for AABB excavation volume.")]
    [SerializeField] private Collider bladeCollider;
    [Tooltip("Transform used for ground sampling depth checks.")]
    [SerializeField] private Transform bladeTransform;

    [Header("Excavation Settings")]
    [Tooltip("Excavation rate in cubic meters per second.")]
    [SerializeField] private float excavateRate = 3f;
    [Tooltip("Particles spawned per cubic meter excavated.")]
    [SerializeField] private float particlePerCubicM = 30f;

    [Header("Spawn Settings")]
    [Tooltip("Prefab for soil particles to spawn.")]
    [SerializeField] private GameObject soilPrefab;
    [Tooltip("Maximum particles spawned per FixedUpdate.")]
    [SerializeField] private int maxParticlesPerFrame = 500;

    [Header("Depth & Layers")]
    [Tooltip("Additional penetration depth offset.")]
    [SerializeField] private float depthOffset = 0.1f;
    [Tooltip("Layer(s) considered terrain for OverlapBox.")]
    [SerializeField] private LayerMask terrainLayer;

    [Header("Mode Angle Thresholds (deg)")]
    [SerializeField] private float dumpMinAngle = -90f;
    [SerializeField] private float dumpMaxAngle = 0f;
    [SerializeField] private float digMinAngle = 0f;
    [SerializeField] private float digMaxAngle = 45f;
    [SerializeField] private float idleMinAngle = 45f;
    [SerializeField] private float idleMaxAngle = 90f;

    [Header("References")]
    [Tooltip("TerrainDeformManager to delegate terrain modifications.")]
    [SerializeField] private TerrainDeformManager deformManager;
    [Tooltip("Terrain used for height sampling.")]
    [SerializeField] private Terrain terrain;
    [Tooltip("Controller providing bucketAngle.")]
    [SerializeField] private ExcavatorController_public excavatorController;
    private BucketGrabberMulti _modeCtrl;

    private Rigidbody _rb;
    private Collider _col;
    private TerrainCollider _terrainCollider;
    private float _particleAccumulator;

    public bool isDigging { get; private set; }

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _col = GetComponent<Collider>();

        if (deformManager == null)
            deformManager = FindObjectOfType<TerrainDeformManager>();

        if (terrain == null && deformManager != null)
            terrain = deformManager.GetComponent<Terrain>();
        _terrainCollider = terrain.GetComponent<TerrainCollider>();

        _modeCtrl = GetComponentInParent<BucketGrabberMulti>();

        if (excavatorController == null)
            excavatorController = FindObjectOfType<ExcavatorController_public>();
    }

    void FixedUpdate()
    {
        UpdateMode();
        DetectDig();

        if (!isDigging || _modeCtrl.CurrentMode != BucketGrabberMulti.Mode.Dig)
            return;

        Bounds bb = bladeCollider.bounds;
        if (Physics.OverlapBox(bb.center, bb.extents, bladeCollider.transform.rotation, terrainLayer).Length == 0)
            return;

        float deltaVol = excavateRate * Time.fixedDeltaTime;
        // ─── penetration 계산 ───
        // bladeCollider.bounds 의 네 귀퉁이 Y값을 모두 샘플링해서
        // 지형보다 아래로 얼마나 내려갔는지 가장 큰 값을 골라냅니다.
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
        penetration = Mathf.Max(0f, penetration);  // 음수 제거

        // ─── LowerRectAABB 호출 ───
        float carved = deformManager.LowerRectAABB(bb.min, bb.max, deltaVol, penetration);

        SpawnParticles(carved, bb);
    }

    private void UpdateMode()
    {
        if (excavatorController == null || _modeCtrl == null) return;

        float angle = excavatorController.BucketAngle;
        var desired = BucketGrabberMulti.Mode.Idle;

        if (angle >= dumpMinAngle && angle < dumpMaxAngle)
            desired = BucketGrabberMulti.Mode.Dump;
        else if (angle >= digMinAngle && angle < digMaxAngle)
            desired = BucketGrabberMulti.Mode.Dig;
        else if (angle >= idleMinAngle && angle <= idleMaxAngle)
            desired = BucketGrabberMulti.Mode.Idle;

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
            float y = terrain.SampleHeight(new Vector3(x, 0f, z))
                      + terrain.transform.position.y + 0.5f;
            var go = Instantiate(soilPrefab, new Vector3(x, y, z), Quaternion.identity);
            if (go.TryGetComponent<Rigidbody>(out var rb))
                rb.mass = 0.1f;
        }
    }
}
