using UnityEngine;

public class ExcavatorController_publicMerged : MonoBehaviour
{
    [Header("Excavator Parts")]
    [SerializeField] private Transform swing;
    [SerializeField] private Transform boom;
    [SerializeField] private Transform arm;
    [SerializeField] private Transform bucket;

    [Header("Rotation Speeds (deg/s)")]
    [SerializeField] private float swingSpeed = 30f;
    [SerializeField] private float boomSpeed = 30f;
    [SerializeField] private float armSpeed = 30f;
    [SerializeField] private float bucketSpeed = 30f;

    [Header("Rotation Limits (deg)")]
    [SerializeField] private Vector2 swingLimits = new Vector2(-90f, 90f);
    [SerializeField] private Vector2 boomLimits = new Vector2(-45f, 45f);
    [SerializeField] private Vector2 armLimits = new Vector2(-90f, 90f);
    [SerializeField] private Vector2 bucketLimits = new Vector2(-90f, 90f);

    private float swingAngle;
    private float boomAngle;
    private float armAngle;
    private float bucketAngle;

    private Quaternion initSwingRot;
    private Quaternion initBoomRot;
    private Quaternion initArmRot;
    private Quaternion initBucketRot;

    /// <summary>
    /// Current bucket angle in degrees, accessible by BucketController.
    /// </summary>
    public float BucketAngle => bucketAngle;

    void Awake()
    {
        initSwingRot  = swing.localRotation;
        initBoomRot   = boom.localRotation;
        initArmRot    = arm.localRotation;
        initBucketRot = bucket.localRotation;
    }

    void Update()
    {
        HandleInput();
        ApplyRotations();
    }

    private void HandleInput()
    {
        float dt = Time.deltaTime;

        // Swing (Q/E)
        if (Input.GetKey(KeyCode.Q)) swingAngle -= swingSpeed * dt;
        if (Input.GetKey(KeyCode.E)) swingAngle += swingSpeed * dt;
        swingAngle = Mathf.Clamp(swingAngle, swingLimits.x, swingLimits.y);

        // Boom (W/S)
        if (Input.GetKey(KeyCode.W)) boomAngle += boomSpeed * dt;
        if (Input.GetKey(KeyCode.S)) boomAngle -= boomSpeed * dt;
        boomAngle = Mathf.Clamp(boomAngle, boomLimits.x, boomLimits.y);

        // Arm (A/D)
        if (Input.GetKey(KeyCode.A)) armAngle += armSpeed * dt;
        if (Input.GetKey(KeyCode.D)) armAngle -= armSpeed * dt;
        armAngle = Mathf.Clamp(armAngle, armLimits.x, armLimits.y);

        // Bucket (R/F)
        if (Input.GetKey(KeyCode.R)) bucketAngle += bucketSpeed * dt;
        if (Input.GetKey(KeyCode.F)) bucketAngle -= bucketSpeed * dt;
        bucketAngle = Mathf.Clamp(bucketAngle, bucketLimits.x, bucketLimits.y);
    }

    private void ApplyRotations()
    {
        // Apply local rotations with initial offsets
        swing.localRotation  = initSwingRot  * Quaternion.AngleAxis(swingAngle,  Vector3.forward);
        boom.localRotation   = initBoomRot   * Quaternion.AngleAxis(boomAngle,   Vector3.up);
        arm.localRotation    = initArmRot    * Quaternion.AngleAxis(armAngle,    Vector3.up);
        bucket.localRotation = initBucketRot * Quaternion.AngleAxis(bucketAngle, Vector3.up);
    }
}
