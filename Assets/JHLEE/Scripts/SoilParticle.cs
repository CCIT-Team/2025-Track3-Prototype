using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
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
    private Collider _col;
    private TerrainRaiseManager  _raiseMgr;
    private float _timer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        // 물리 머티리얼로 마찰력 적용
        var mat = new PhysicMaterial(name + "_PhysMat")
        {
            dynamicFriction = dynamicFrictionCoef,
            staticFriction  = staticFrictionCoef,
            frictionCombine = PhysicMaterialCombine.Maximum
        };
        _col.material = mat;

        // TerrainRaiseManager 참조
        _raiseMgr = FindObjectOfType<TerrainRaiseManager>();
    }

    void Start()
    {
        // Rigidbody 드래그 초기값
        _rb.drag = dynamicFrictionCoef;
        _rb.angularDrag = dynamicFrictionCoef;
    }

    void Update()
    {
        if (_rb.isKinematic || transform.parent != null)
        {
            _timer = 0f;
            return;
        }

        // Terrain 높이 보정
        Terrain terrain = Terrain.activeTerrain;
        float terrainY = terrain.SampleHeight(transform.position) + terrain.GetPosition().y;
        if (transform.position.y < terrainY + 0.01f)
        {
            var pos = transform.position;
            pos.y = terrainY + 0.01f;
            transform.position = pos;
            if (!_rb.isKinematic)
            {
                // 1) 속도 초기화
                _rb.velocity        = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                // 2) 물리 제약 변경
                _rb.isKinematic = true;  
            }
        }

        // Rest 감지 후 Terrain에 등록
        if (_rb.velocity.magnitude < restThreshold)
        {
            _timer += Time.deltaTime;
            if (_timer >= restTime && _raiseMgr != null)
            {
                _raiseMgr.RegisterStop(gameObject);
                _timer = 0f;
            }
        }
        else _timer = 0f;
    }

    void OnCollisionStay(Collision col)
    {
        if (_rb.isKinematic) return;
        if (col.collider.gameObject.layer != LayerMask.NameToLayer("Terrain")) return;

        Vector3 avgNormal = Vector3.zero;
        foreach (var ct in col.contacts) avgNormal += ct.normal;
        avgNormal.Normalize();

        float slopeAngle = Vector3.Angle(avgNormal, Vector3.up);
        float g = Physics.gravity.magnitude;
        float downhillAccel = g * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
        float normalAccel   = g * Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
        float maxStaticAccel = staticFrictionCoef * normalAccel;

        Vector3 velXZ = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        if (downhillAccel < maxStaticAccel && velXZ.magnitude < stopThreshold)
        {
            // 정지
            _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            _rb.angularVelocity = Vector3.zero;
            _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            // 운동 마찰
            _rb.constraints = RigidbodyConstraints.None;
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (col.collider.gameObject.layer != LayerMask.NameToLayer("Raindrops")) return;
        _rb.constraints = RigidbodyConstraints.None;
    }
}
