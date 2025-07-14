using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BucketController : MonoBehaviour
{
    [Header("굴착용 Blade Collider")]  
    public Collider bladeCollider;        // AABB 추출용
    public Transform bladeTransform;      // 깊이 판정용

    [Header("굴착 설정")]
    public float excavateRate = 3f;     // m³/s
    public float particlePerCubicM = 30f;  // m³당 입자 수
    public GameObject soilPrefab;
    public LayerMask terrainLayer;        // Terrain 레이어
    public float depthOffset = 0.1f;     // 굴착 판정 오프셋

    [Header("입자 생성 제한")]
    public int maxParticlesPerFrame = 500;

    [Header("모드 자동 전환 임계값 (도)")]
    // 덤프: -90° ~ 0°, DIG: 0° ~ 45°, IDLE: 45° ~ 90°
    public float dumpMinAngle = -90f;
    public float dumpMaxAngle = 0f;
    public float digMinAngle = 0f;
    public float digMaxAngle = 45f;
    public float idleMinAngle = 45f;
    public float idleMaxAngle = 90f;

    [Header("굴착기 컨트롤러 참조")]
    public ExcavatorController_public excavatorController;  // Inspector에서 직접 할당 또는 자동 검색

    private float _particleAccumulator;
    private Rigidbody _rb;
    private Collider _col;
    private TerrainDeformManager _deformer;
    private TerrainCollider _terrainCol;
    private Terrain _terrain;
    private BucketGrabberMulti _modeCtrl;

    public bool isDigging { get; private set; }

    void Start()
    {
        // Rigidbody 설정
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Collider
        _col = GetComponent<Collider>();
        if (_col == null)
            Debug.LogWarning($"[BucketController] Collider missing on '{gameObject.name}'");

        // TerrainDeformManager
        _deformer = FindObjectOfType<TerrainDeformManager>();
        if (_deformer == null)
        {
            Debug.LogError("[BucketController] TerrainDeformManager not found");
            enabled = false;
            return;
        }
        _terrainCol = _deformer.GetComponent<TerrainCollider>();
        _terrain = _deformer.GetComponent<Terrain>();

        // BucketGrabberMulti
        _modeCtrl = GetComponentInParent<BucketGrabberMulti>();
        if (_modeCtrl == null)
            Debug.LogError("[BucketController] BucketGrabberMulti not found");

        // ExcavatorController 할당 여부 확인
        if (excavatorController == null)
        {
            excavatorController = FindObjectOfType<ExcavatorController_public>();
            if (excavatorController == null)
                Debug.LogError("[BucketController] ExcavatorController_public not found in scene or inspector");
        }
    }

    void FixedUpdate()
    {
        // 0) 버킷 각도 기반 모드 자동 전환
        if (excavatorController != null && _modeCtrl != null)
        {
            float angle = excavatorController.bucketAngle;
            BucketGrabberMulti.Mode desired = BucketGrabberMulti.Mode.Idle;
            if (angle >= dumpMinAngle && angle < dumpMaxAngle)
                desired = BucketGrabberMulti.Mode.Dump;
            else if (angle >= digMinAngle && angle < digMaxAngle)
                desired = BucketGrabberMulti.Mode.Dig;
            else if (angle >= idleMinAngle && angle <= idleMaxAngle)
                desired = BucketGrabberMulti.Mode.Idle;

            if (_modeCtrl.CurrentMode != desired)
                _modeCtrl.SetMode(desired);
        }

        // 1) Dig 판정
        float bladeY = bladeCollider.bounds.min.y;
        float groundY = _terrain.SampleHeight(bladeTransform.position) + _terrain.GetPosition().y;
        isDigging = (groundY - bladeY) > depthOffset;

        if (_col != null && _terrainCol != null)
            Physics.IgnoreCollision(_col, _terrainCol, isDigging);

        // 2) Dig 모드가 아니면 종료
        if (_modeCtrl == null || _modeCtrl.CurrentMode != BucketGrabberMulti.Mode.Dig || !isDigging)
            return;

        // 3) Terrain 변형 및 입자 생성
        Bounds bb = bladeCollider.bounds;
        Collider[] hits = Physics.OverlapBox(bb.center, bb.extents, bladeCollider.transform.rotation, terrainLayer);
        if (hits.Length == 0) return;

        float deltaVol = excavateRate * Time.fixedDeltaTime;
        float penetrationDepth = Mathf.Max(0f, groundY - bladeY);
        float carved = _deformer.LowerRectAABB(bb.min, bb.max, deltaVol, penetrationDepth);

        if (particlePerCubicM <= 0f)
        {
            _particleAccumulator = 0f;
            return;
        }
        _particleAccumulator += carved * particlePerCubicM;
        int toSpawn = Mathf.FloorToInt(_particleAccumulator);
        _particleAccumulator -= toSpawn;
        toSpawn = Mathf.Min(toSpawn, maxParticlesPerFrame);

        for (int i = 0; i < toSpawn; i++)
        {
            float x = Random.Range(bb.min.x, bb.max.x);
            float z = Random.Range(bb.min.z, bb.max.z);
            float y = _terrain.SampleHeight(new Vector3(x, 0f, z)) + _terrain.GetPosition().y + 0.5f;
            GameObject go = Instantiate(soilPrefab, new Vector3(x, y, z), Quaternion.identity);
            if (go.TryGetComponent<Rigidbody>(out var rb)) rb.mass = 0.1f;
        }
    }
}
