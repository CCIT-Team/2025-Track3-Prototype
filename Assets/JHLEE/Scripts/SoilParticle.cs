using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SoilParticle : MonoBehaviour
{
    [Header("마찰 계수")]
    [Tooltip("정지 마찰 계수 (static μ)")]
    public float staticFrictionCoef  = 0.8f; 
    [Tooltip("이동 마찰 계수 (dynamic μ)")]
    public float dynamicFrictionCoef = 0.9f;

    [Header("정지 임계 조건")]
    public float stopThreshold = 0.02f;   // 평면 속도 임계
    public float restThreshold = 0.05f;   // 속도 Rest 임계
    public float restTime      = 0.5f;    // Rest 유지 시간

    [Header("쌓기 반경 (m)")]
    [Tooltip("Terrain에 베이크할 반경 (m)")]
    public float bakeRadius    = 0.3f;
    
    [Header("높이 추가량 (m)")]
    [Tooltip("입자가 쌓일 때 terrain 높이에 더해질 고정 오프셋")]
    public float heightOffset  = 0.3f;

    private Rigidbody _rb;
    private TerrainRaiseManager  _raiseMgr;
    private float                _timer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // TerrainRaiseManager 참조
        _raiseMgr = FindObjectOfType<TerrainRaiseManager>();
    }

    void Start()
    {
        _rb.drag = 0f;
        _rb.angularDrag = 0f;
    }

    void Update()
    {
        if (_rb == null || _rb.isKinematic || transform.parent != null)
        {
            _timer = 0f;
            return;
        }

        // Terrain 높이 샘플링
        Terrain terrain = Terrain.activeTerrain;
        float terrainY = terrain.SampleHeight(transform.position) + terrain.GetPosition().y;

        // 땅 아래 침투 보정
        if (transform.position.y < terrainY + 0.01f)
        {
            Vector3 pos = transform.position;
            pos.y = terrainY + 0.01f;
            transform.position = pos;
            // kinematic 아닐 때만 속도 초기화
            if (!_rb.isKinematic)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        // Rest 감지 후 매니저에 등록
        if (_rb.velocity.magnitude < restThreshold)
        {
            _timer += Time.deltaTime;
            if (_timer >= restTime && _raiseMgr != null)
            {
                _raiseMgr.RegisterStop(gameObject);
                _timer = 0f;
            }
        }
        else
        {
            _timer = 0f;
        }
    }

    void OnCollisionStay(Collision col)
    {
        if (_rb == null) return;
        // kinematic 상태일 경우 물리 처리 생략
        if (_rb.isKinematic) return;
        if (col.collider == null) return;
        if (col.collider.gameObject.layer != LayerMask.NameToLayer("Terrain")) return;

        Vector3 avgNormal = Vector3.zero;
        foreach (var ct in col.contacts)
            avgNormal += ct.normal;
        avgNormal.Normalize();

        float slopeAngle = Vector3.Angle(avgNormal, Vector3.up);
        float g = Physics.gravity.magnitude;
        float downhillAccel = g * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
        float normalAccel   = g * Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
        float maxStaticAccel = staticFrictionCoef * normalAccel;

        Vector3 velXZ = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        if (downhillAccel < maxStaticAccel && velXZ.magnitude < stopThreshold)
        {
            // XZ 방향만 정지
            Vector3 v = _rb.velocity;
            _rb.velocity = new Vector3(0f, v.y, 0f);
            _rb.angularVelocity = Vector3.zero;
            _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            _rb.constraints = RigidbodyConstraints.None;
            float normalForce = _rb.mass * normalAccel;
            Vector3 frictionDir = velXZ.magnitude > 0f ? -velXZ.normalized : Vector3.zero;
            Vector3 friction = frictionDir * (dynamicFrictionCoef * normalForce);
            _rb.AddForce(friction, ForceMode.Force);
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (col.collider == null) return;
        if (col.collider.gameObject.layer != LayerMask.NameToLayer("Terrain")) return;
        _rb.constraints = RigidbodyConstraints.None;
    }
}
