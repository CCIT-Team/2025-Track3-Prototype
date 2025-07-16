using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SoilParticleMerged : MonoBehaviour
{
    [Header("Friction Coefficients")]
    [SerializeField, Tooltip("Static friction coefficient (μ)")] private float staticFriction = 0.8f;
    [SerializeField, Tooltip("Dynamic friction coefficient (μ)")] private float dynamicFriction = 0.9f;

    [Header("Rest Thresholds")]
    [SerializeField, Tooltip("Velocity magnitude below which rest timer starts")] private float restThreshold = 0.05f;
    [SerializeField, Tooltip("Time in seconds at low velocity to consider stopped")] private float restTime = 0.5f;

    [Header("Bake Settings")]
    [SerializeField, Tooltip("Radius (m) for terrain baking")] public float bakeRadius = 0.3f;
    [SerializeField, Tooltip("Height offset (m) added when baking")] public float heightOffset = 0.3f;

    [Header("Physics Material")]
    [SerializeField, Tooltip("Combine mode for friction material")] private PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Maximum;

    private Rigidbody _rb;
    private Collider _col;
    private TerrainRaiseManagerMerged _raiseManager;
    private float _restTimer;
    private const float epsilon = 0.01f;

    private TerrainLayer _layer;//?

    public void SetLayer(TerrainLayer layer)
    {
        _layer = layer;
    }

    public TerrainLayer GetLayer()
    {
        return _layer;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        // Create and assign a PhysicMaterial for friction
        var mat = new PhysicMaterial(name + "_PhysMat")
        {
            staticFriction = staticFriction,
            dynamicFriction = dynamicFriction,
            frictionCombine = frictionCombine
        };
        _col.material = mat;

        _raiseManager = FindObjectOfType<TerrainRaiseManagerMerged>();
    }

    void Start()
    {
        // Rigidbody 드래그 초기값
        _rb.drag = dynamicFriction;
        _rb.angularDrag = dynamicFriction;
    }

    void Update()
    {
        if (_rb.isKinematic || transform.parent != null)
        {
            _restTimer = 0f;
            return;
        }

        // Ensure particle stays above surface
        float surfaceY = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y;
        if (transform.position.y < surfaceY + epsilon)
        {
            var p = transform.position;
            p.y = surfaceY + epsilon;
            transform.position = p;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            // 착지 즉시 멈춤 등록
            TryRegisterStop(surfaceY);
            _restTimer = 0f;
            return;
        }

        // Check rest
        if (_rb.velocity.magnitude < restThreshold)
        {
            _restTimer += Time.deltaTime;
            if (_restTimer >= restTime)
            {
                TryRegisterStop(surfaceY);
                _restTimer = 0f;
            }
        }
        else _restTimer = 0f;
    }

    //정지 조건 감지 및 파괴
    private void TryRegisterStop(float surfaceY)
    {
        float bottomY = _col.bounds.min.y + heightOffset;
        // 무조건 등록만 하고, Destroy는 하지 않음
        if (_raiseManager != null)
            _raiseManager.RegisterStop(gameObject);
    }

    void OnCollisionStay(Collision col)
    {
        if (_rb.isKinematic || col.collider.gameObject.layer != LayerMask.NameToLayer("SoilParticle")) return;

        // Calculate slope and static friction
        Vector3 avgNormal = Vector3.zero;
        foreach (var contact in col.contacts) avgNormal += contact.normal;
        avgNormal.Normalize();

        float slopeAngle = Vector3.Angle(avgNormal, Vector3.up);
        float g = Physics.gravity.magnitude;
        float downhillAccel = g * Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
        float normalAccel = g * Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
        float maxStaticAccel = staticFriction * normalAccel;

        Vector3 velXZ = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        if (downhillAccel < maxStaticAccel && velXZ.magnitude < restThreshold)
        {
            // Freeze horizontal motion
            _rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            _rb.constraints = RigidbodyConstraints.None;
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.collider.gameObject.layer != LayerMask.NameToLayer("SoilParticle")) return;
        _rb.constraints = RigidbodyConstraints.None;
    }
}
