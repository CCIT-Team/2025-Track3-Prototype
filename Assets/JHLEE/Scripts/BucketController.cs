using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BucketController : MonoBehaviour
{
    [Header("굴착용 Blade Collider")]
    public Collider bladeCollider;      // AABB 추출용
    public Transform bladeTransform;    // 깊이 판정용

    [Header("굴착 설정")]
    public float bucketRadius = 1f;
    public float excavateRate = 0.5f;      // m³/s
    public float particlePerCubicM = 0f;   // m³당 입자 수
    public GameObject soilPrefab;
    public LayerMask terrainLayer;         // Terrain 레이어 마스크
    public float depthOffset = 0.02f;      // 굴착 판정 오프셋

    [Header("프레임당 최대 입자 생성 수")]
    public int maxParticlesPerFrame = 20;

    [Header("이동 설정 (AddForce)")]
    public float moveSpeed = 5f;
    public float accel = 20f;
    public float decelFactor = 0.8f;

    [Header("수직 이동 설정 (AddForce)")]
    public float liftSpeed = 3f;
    public float liftAccel = 20f;
    public float liftDecelFactor = 0.8f;

    private float _particleAccumulator = 0f;
    private Rigidbody _rb;
    private Collider _col;
    private TerrainDeformManager _deformer;
    private TerrainCollider _terrainCol;
    private Terrain _terrain;
    private BucketGrabberMulti _modeCtrl;
    public bool isDigging;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = false;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _col = GetComponent<Collider>();
        _deformer = FindObjectOfType<TerrainDeformManager>();
        _terrainCol = _deformer.GetComponent<TerrainCollider>();
        _terrain = _deformer.GetComponent<Terrain>();
        _modeCtrl = GetComponent<BucketGrabberMulti>() 
                    ?? GetComponentInParent<BucketGrabberMulti>() 
                    ?? GetComponentInChildren<BucketGrabberMulti>();
    }

    void FixedUpdate()
    {
        // 1) 이동 로직 (X-Z 평면)
        Vector3 inpXZ = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        Vector3 desiredXZ = inpXZ.normalized * moveSpeed;
        Vector3 currentXZ = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        Vector3 deltaXZ = desiredXZ - currentXZ;
        Vector3 forceXZ = inpXZ.sqrMagnitude > 0.01f
            ? Vector3.ClampMagnitude(deltaXZ * _rb.mass / Time.fixedDeltaTime, accel * _rb.mass)
            : -currentXZ * decelFactor * _rb.mass;
        _rb.AddForce(forceXZ, ForceMode.Force);

        // 2) 수직 이동 로직 (Y축)
        float yIn = Input.GetKey(KeyCode.O) ? 1f
                  : Input.GetKey(KeyCode.L) ? -1f
                  : 0f;
        float desiredY = yIn * liftSpeed;
        float deltaY = desiredY - _rb.velocity.y;
        Vector3 forceY = Mathf.Abs(yIn) > 0.01f
            ? Vector3.up * Mathf.Clamp(deltaY * _rb.mass / Time.fixedDeltaTime, -liftAccel * _rb.mass, liftAccel * _rb.mass)
            : Vector3.up * (-_rb.velocity.y * liftDecelFactor * _rb.mass);
        _rb.AddForce(forceY, ForceMode.Force);
        
        // 2) Dig 모드 & isDigging 체크
        float bladeY = bladeCollider.bounds.min.y;
        float groundY = _terrain.SampleHeight(bladeTransform.position) + _terrain.GetPosition().y;
        isDigging = (groundY - bladeY) > depthOffset;
        Physics.IgnoreCollision(_col, _terrainCol, isDigging);

        bool canDig = _modeCtrl != null 
                    && _modeCtrl.CurrentMode == BucketGrabberMulti.Mode.Dig 
                    && isDigging;
        if (!canDig) return;

        // 3) Terrain 변형
        Bounds bb = bladeCollider.bounds;
        Collider[] hits = Physics.OverlapBox(
            bb.center, bb.extents, bladeCollider.transform.rotation, terrainLayer);
        if (hits.Length == 0) return;

        float deltaVol = excavateRate * Time.fixedDeltaTime;
        float penetrationDepth = Mathf.Max(0f, groundY - bladeY);
        float carved = _deformer.LowerRectAABB(bb.min, bb.max, deltaVol, penetrationDepth);

        // 4) particlePerCubicM == 0 인 경우 스폰 및 누적 리셋
        if (particlePerCubicM <= 0f)
        {
            _particleAccumulator = 0f;
            return;
        }

        // 5) 입자 누적 및 생성
        _particleAccumulator += carved * particlePerCubicM;
        int toSpawn = Mathf.FloorToInt(_particleAccumulator);
        _particleAccumulator -= toSpawn;

        // 프레임당 최대 생성 수 제한
        toSpawn = Mathf.Min(toSpawn, maxParticlesPerFrame);

        for (int i = 0; i < toSpawn; i++)
        {
            float x = Random.Range(bb.min.x, bb.max.x);
            float z = Random.Range(bb.min.z, bb.max.z);
            float y = _terrain.SampleHeight(new Vector3(x, 0f, z)) 
                    + _terrain.GetPosition().y + 0.5f;
            GameObject go = Instantiate(soilPrefab, new Vector3(x, y, z), Quaternion.identity);
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.mass = 0.1f;
        }
    }
}
