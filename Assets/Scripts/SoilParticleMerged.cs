using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SoilParticleMerged : MonoBehaviour
{
    [Header("Friction Coefficients")]
    [SerializeField, Tooltip("Static friction coefficient (μ)")] private float staticFriction = 1.2f;
    [SerializeField, Tooltip("Dynamic friction coefficient (μ)")] private float dynamicFriction = 1.0f;

    [Header("Rest Thresholds")]
    [SerializeField, Tooltip("Velocity magnitude below which rest timer starts")] private float restThreshold = 0.05f;
    [SerializeField, Tooltip("Time in seconds at low velocity to consider stopped")] private float restTime = 0.5f;

    [Header("Freeze Slope Settings")]
    [SerializeField, Tooltip("이 각도 이하 경사면에서만 흙 입자가 감속 처리됩니다")] private float freezeSlopeAngleDeg = 75f;

    [Header("Bake Settings (for painting)")]
    [SerializeField, Tooltip("Radius (m) for terrain baking")] public float bakeRadius = 0.3f;
    [SerializeField, Tooltip("Height offset (m) added when baking")] public float heightOffset = 0.3f;

    [Header("Auto Destroy Settings")]
    [SerializeField, Tooltip("지형 아래 이 높이(m) 이하로 떨어지면 즉시 삭제")] private float destroyBelowOffset = 0.1f;

    [Header("Ground Detection")]
    [SerializeField, Tooltip("메시 지형을 레이캐스트로 검출할 레이어")]
    private LayerMask groundLayerMask;

    [Header("Physics Material")]
    [SerializeField, Tooltip("Combine mode for friction material")] private PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Maximum;

    private Rigidbody _rb;
    private Collider _col;
    private float _restTimer;
    private const float epsilon = 0.01f;

    /// <summary>
    /// 버킷에 잡힌 상태인지 표시하는 플래그
    /// </summary>
    [HideInInspector]
    public bool IsGrabbed = false;

    // 버킷 접촉 플래그
    private bool _isTouchingBucket;

    // Grab된 입자 감지: 부모화 상태 검사 via transform.parent

    // —— TerrainLayer 필드 및 접근자 추가 ——
    private TerrainLayer _layer;
    public void SetLayer(TerrainLayer layer) => _layer = layer;
    public TerrainLayer GetLayer() => _layer;


    // IPoolable (optional): 제거 로직에서 활용하지 않으면 생략 가능
    private GameObjectPool _pool;
    public void SetPoolInstance(GameObjectPool pool) => _pool = pool;
    public bool ComparePoolInstance(GameObjectPool pool) => _pool == pool;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        // PhysicMaterial 세팅
        var mat = new PhysicMaterial(name + "_SoilPhysMat")
        {
            staticFriction = staticFriction,
            dynamicFriction = dynamicFriction,
            frictionCombine = frictionCombine,
            bounciness = 0f,
            bounceCombine = PhysicMaterialCombine.Minimum
        };
        _col.material = mat;
    }

    void Start()
    {
        _rb.drag = dynamicFriction;
        _rb.angularDrag = dynamicFriction;
    }

    void Update()
    {
        // 1) Skip conditions: Grabbed, kinematic (already settled), or touching bucket
        if (!CompareTag("SoilParticle") || _rb.isKinematic || transform.parent != null || _isTouchingBucket)
        {
            _restTimer = 0f;
            return;
        }

        // 2) Get surface height
        if (!TryGetSurfaceY(out float surfaceY))
        {
            DestroySelf();
            return;
        }

        // 3) Below ground by threshold -> destroy
        if (transform.position.y < surfaceY - destroyBelowOffset)
        {
            DestroySelf();
            return;
        }

        // 4) Slightly below surface -> snap and settle
        if (transform.position.y < surfaceY + epsilon)
        {
            Vector3 p = transform.position;
            p.y = surfaceY + epsilon;
            transform.position = p;

            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeAll;

            _restTimer = 0f;
            return;
        }

        // 5) Otherwise let physics simulation run
    }

    private bool TryGetSurfaceY(out float surfaceY)
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up;
        if (Physics.Raycast(origin, Vector3.down, out hit, Mathf.Infinity, groundLayerMask))
        {
            surfaceY = hit.point.y;
            return true;
        }
        if (Terrain.activeTerrain != null)
        {
            surfaceY = Terrain.activeTerrain.SampleHeight(transform.position)
                         + Terrain.activeTerrain.transform.position.y;
            return true;
        }
        surfaceY = transform.position.y;
        return false;
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bucket"))
            _isTouchingBucket = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Bucket"))
            _isTouchingBucket = false;
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Bucket"))
        {
            _isTouchingBucket = true;
            return;
        }
        if (_rb.isKinematic) return;
        if (!col.collider.CompareTag("SoilParticle")) return;

        // 충돌 시 속도 보정
        Vector3 normal = col.contacts[0].normal;
        Vector3 v = _rb.velocity;
        Vector3 comp = Vector3.Project(v, normal);
        _rb.velocity = v - comp;
        _rb.angularVelocity = Vector3.zero;
    }

    void OnCollisionStay(Collision col)
    {
        if (_rb.isKinematic) return;
        if (!col.collider.CompareTag("SoilParticle"))
        {
            _restTimer = 0f;
            return;
        }

        bool hasSupport = false;
        Vector3 avgNorm = Vector3.zero;
        foreach (ContactPoint cp in col.contacts)
        {
            avgNorm += cp.normal;
            if (!hasSupport && Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                hasSupport = true;
        }
        if (!hasSupport) { _restTimer = 0f; return; }

        avgNorm.Normalize();
        float slopeAng = Vector3.Angle(avgNorm, Vector3.up);
        if (slopeAng > freezeSlopeAngleDeg) { _restTimer = 0f; return; }

        // 속도 감쇠
        Vector3 vel = _rb.velocity;
        vel.x *= 0.1f; vel.z *= 0.1f;
        _rb.velocity = vel;
        _rb.angularVelocity = Vector3.zero;

        // 정지 판정
        if (vel.sqrMagnitude < restThreshold * restThreshold)
        {
            _restTimer += Time.deltaTime;
            if (_restTimer >= restTime)
            {
                Vector3 supportPt = Vector3.zero;
                foreach (ContactPoint cp in col.contacts)
                {
                    if (Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                    {
                        supportPt = cp.point;
                        break;
                    }
                }
                Vector3 p = transform.position;
                p.y = supportPt.y + epsilon;
                transform.position = p;

                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
                _rb.constraints = RigidbodyConstraints.FreezeAll;

                _restTimer = 0f;
            }
        }
        else
        {
            _restTimer = 0f;
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.collider.CompareTag("Bucket"))
        {
            _isTouchingBucket = false;
            return;
        }
        if (col.collider.CompareTag("SoilParticle"))
            _rb.constraints = RigidbodyConstraints.None;
    }
}
