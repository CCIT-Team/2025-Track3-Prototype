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
    private LayerMask groundLayerMask = ~0;

    [Header("Physics Material")]
    [SerializeField, Tooltip("Combine mode for friction material")] private PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Maximum;

    [Header("Pooling (safety)")]
    [SerializeField, Tooltip("풀 미연결 시 Destroy로 폴백")] private bool fallbackDestroyIfNoPool = true;

    private Rigidbody _rb;
    private Collider _col;
    private float _restTimer;
    private const float epsilon = 0.01f;

    [HideInInspector] public bool IsGrabbed = false; // 버킷에 잡힘 표시
    private bool _isTouchingBucket;

    // ---- TerrainLayer for painting ----
    private TerrainLayer _layer;
    public void SetLayer(TerrainLayer layer) => _layer = layer;
    public TerrainLayer GetLayer() => _layer;

    // ---- Pool ----
    private GameObjectPool _pool;
    private bool _isPooled = false;
    private bool _returnedOrDestroyed = false; // 중복 반납/삭제 방지

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        if (_col != null)
        {
            var mat = new PhysicMaterial($"{name}_SoilPhysMat")
            {
                staticFriction = staticFriction,
                dynamicFriction = dynamicFriction,
                frictionCombine = frictionCombine,
                bounciness = 0f,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
            _col.material = mat;
        }
        else
        {
            Debug.LogError("[SoilParticleMerged] Collider가 없습니다.", this);
        }

        if (_rb == null)
        {
            Debug.LogError("[SoilParticleMerged] Rigidbody가 없습니다.", this);
        }
    }

    void Start()
    {
        if (_rb != null)
        {
            _rb.drag = dynamicFriction;
            _rb.angularDrag = dynamicFriction;
        }
    }

    void OnEnable()
    {
        _returnedOrDestroyed = false;
        _isTouchingBucket = false;

        if (_col != null) _col.enabled = true;
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.constraints = RigidbodyConstraints.None;
            _rb.mass = Mathf.Max(0.01f, _rb.mass <= 0f ? 0.1f : _rb.mass);
        }
        gameObject.tag = "SoilParticle";
    }

    void OnDisable()
    {
        // 풀 반납 후 재활용 시 초기화 잔여물 제거
        _restTimer = 0f;
        IsGrabbed = false;
        _isTouchingBucket = false;

        Destroy(gameObject);
    }

    void Update()
    {
        if (_rb == null) return; // 방어

        // 1) Skip: 잡힘/키네마틱/부모있음/버킷접촉
        if (!CompareTag("SoilParticle") || _rb.isKinematic || transform.parent != null || _isTouchingBucket)
        {
            _restTimer = 0f;
            return;
        }

        // 2) 지면 탐지 실패 → 안전 제거
        if (!TryGetSurfaceY(out float surfaceY))
        {
            DestroySelf();
            return;
        }

        // 3) 지형 아래로 일정 이하 → 제거
        if (transform.position.y < surfaceY - destroyBelowOffset)
        {
            DestroySelf();
            return;
        }

        // 4) 표면 살짝 아래 → 스냅 후 정지
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
        // 5) else: 자유 낙하/슬라이딩
    }

    private bool TryGetSurfaceY(out float surfaceY)
    {
        // 메시 지면 우선
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hit, Mathf.Infinity, groundLayerMask))
        {
            surfaceY = hit.point.y;
            return true;
        }
        // Terrain 폴백
        if (Terrain.activeTerrain != null)
        {
            surfaceY = Terrain.activeTerrain.SampleHeight(transform.position)
                       + Terrain.activeTerrain.transform.position.y;
            return true;
        }
        surfaceY = transform.position.y; // 마지막 폴백
        return false;
    }

    private void DestroySelf()
    {
        if (_returnedOrDestroyed) return; // 중복 방지
        _returnedOrDestroyed = true;

        // 풀 경로(정상): 풀 인스턴스 있고, 이 오브젝트가 풀 소속이라고 표시된 경우
        if (_pool != null && _isPooled)
        {
            _pool.ReturnGameObject(gameObject);
            return;
        }

        // 폴백: 풀 미연결 or 풀 소속 표시가 안 된 경우
        if (fallbackDestroyIfNoPool)
        {
            Destroy(gameObject);
        }
        else
        {
            // 풀 미연결인데 Destroy를 원치 않으면 비활성화만
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bucket")) _isTouchingBucket = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Bucket")) _isTouchingBucket = false;
    }

    void OnCollisionEnter(Collision col)
    {
        if (_rb == null) return;

        if (col.collider.CompareTag("Bucket"))
        {
            _isTouchingBucket = true;
            return;
        }
        if (_rb.isKinematic) return;
        if (!col.collider.CompareTag("SoilParticle")) return;

        // 충돌 속도 보정
        var normal = col.contacts[0].normal;
        var v = _rb.velocity;
        var comp = Vector3.Project(v, normal);
        _rb.velocity = v - comp;
        _rb.angularVelocity = Vector3.zero;
    }

    void OnCollisionStay(Collision col)
    {
        if (_rb == null || _rb.isKinematic) return;

        if (!col.collider.CompareTag("SoilParticle"))
        {
            _restTimer = 0f;
            return;
        }

        bool hasSupport = false;
        Vector3 avgNorm = Vector3.zero;
        foreach (var cp in col.contacts)
        {
            avgNorm += cp.normal;
            if (!hasSupport && Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                hasSupport = true;
        }
        if (!hasSupport) { _restTimer = 0f; return; }

        avgNorm.Normalize();
        float slopeAng = Vector3.Angle(avgNorm, Vector3.up);
        if (slopeAng > freezeSlopeAngleDeg) { _restTimer = 0f; return; }

        // 감쇠
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
                Vector3 supportPt = transform.position;
                foreach (var cp in col.contacts)
                {
                    if (Vector3.Dot(cp.normal, Vector3.up) > 0.5f)
                    { supportPt = cp.point; break; }
                }

                var p = transform.position;
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
        if (_rb == null) return;

        if (col.collider.CompareTag("Bucket"))
        {
            _isTouchingBucket = false;
            return;
        }
        if (col.collider.CompareTag("SoilParticle"))
            _rb.constraints = RigidbodyConstraints.None;
    }
}
