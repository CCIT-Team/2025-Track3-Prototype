using UnityEngine;

public class ExcavatorController : MonoBehaviour
{
    [Header("Excavator Parts")]
    public Transform swing;        // Body.003
    public Transform boom;         // Main_Forks
    public Transform arm;          // Main_Supporter
    public Transform bucket;       // Plane.003

    [Header("Rotation Settings")]
    public float swingSpeed = 30f;
    public float boomSpeed = 30f;
    public float armSpeed = 30f;
    public float bucketSpeed = 30f;

    [Header("Rotation Angle Limits")]
    public float minSwingAngle = -90f, maxSwingAngle = 90f;
    public float minBoomAngle = -45f, maxBoomAngle = 45f;
    public float minArmAngle = -90f, maxArmAngle = 90f;
    public float minBucketAngle = -120f, maxBucketAngle = 45f;

    private float swingAngle = 0f;
    private float boomAngle = 0f;
    private float armAngle = 0f;
    private float bucketAngle = 0f;

    private Quaternion initSwingLocalRot;
    private Quaternion initBoomLocalRot;
    private Quaternion initArmLocalRot;
    private Quaternion initBucketLocalRot;

    void Start()
    {
        // 각 부품의 초기 로컬 회전값을 저장
        initSwingLocalRot = swing.localRotation;
        initBoomLocalRot = boom.localRotation;
        initArmLocalRot = arm.localRotation;
        initBucketLocalRot = bucket.localRotation;
    }

    void Update()
    {
        HandleInput();
        ApplyRotation();
    }

    void HandleInput()
    {
        float dt = Time.deltaTime;

        // 스윙 (Q / E)
        if (Input.GetKey(KeyCode.Q)) swingAngle -= swingSpeed * dt;
        if (Input.GetKey(KeyCode.E)) swingAngle += swingSpeed * dt;
        swingAngle = Mathf.Clamp(swingAngle, minSwingAngle, maxSwingAngle);

        // 붐 (W / S)
        if (Input.GetKey(KeyCode.W)) boomAngle += boomSpeed * dt;
        if (Input.GetKey(KeyCode.S)) boomAngle -= boomSpeed * dt;
        boomAngle = Mathf.Clamp(boomAngle, minBoomAngle, maxBoomAngle);

        // 암 (A / D)
        if (Input.GetKey(KeyCode.A)) armAngle += armSpeed * dt;
        if (Input.GetKey(KeyCode.D)) armAngle -= armSpeed * dt;
        armAngle = Mathf.Clamp(armAngle, minArmAngle, maxArmAngle);

        // 버킷 (R / F)
        if (Input.GetKey(KeyCode.R)) bucketAngle += bucketSpeed * dt;
        if (Input.GetKey(KeyCode.F)) bucketAngle -= bucketSpeed * dt;
        bucketAngle = Mathf.Clamp(bucketAngle, minBucketAngle, maxBucketAngle);
    }

    void ApplyRotation()
    {
        // Swing: Z축 (Vector3.forward)
        swing.localRotation = initSwingLocalRot * Quaternion.AngleAxis(swingAngle, Vector3.forward);

        // Boom: X축 (Vector3.up)
        boom.localRotation = initBoomLocalRot * Quaternion.AngleAxis(boomAngle, Vector3.up);

        // Arm: Y축 (Vector3.up)
        arm.localRotation = initArmLocalRot * Quaternion.AngleAxis(armAngle, Vector3.up);

        // Bucket: Y축 (Vector3.up)
        bucket.localRotation = initBucketLocalRot * Quaternion.AngleAxis(bucketAngle, Vector3.up);
    }
}