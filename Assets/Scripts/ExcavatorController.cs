using UnityEngine;

public class ExcavatorController : MonoBehaviour
{
    [Header("Excavator Parts")]
    public Transform swing;        // Body.003 - 굴착기 차체 (좌우 회전)
    public Transform boom;         // Main_Forks - 주 팔 (상하 움직임)
    public Transform arm;          // Main_Supporter - 보조 팔 (붐에 연결)
    public Transform bucket;       // Plane.003 - 버킷 (암에 연결)

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

    // 현재 각 파트의 목표 회전 각도
    private float swingAngle = 0f;
    private float boomAngle = 0f;
    private float armAngle = 0f;
    public float bucketAngle = 0f;

    // 각 파트의 초기 로컬 회전값 (상대적인 움직임을 위해 필요)
    private Quaternion initSwingLocalRot;
    private Quaternion initBoomLocalRot;
    private Quaternion initArmLocalRot;
    private Quaternion initBucketLocalRot;
    
    void Start()
    {
        // 각 굴착기 부품의 초기 로컬 회전값을 저장
        initSwingLocalRot = swing.localRotation;
        initBoomLocalRot = boom.localRotation;
        initArmLocalRot = arm.localRotation;
        initBucketLocalRot = bucket.localRotation;

        // Start 시점에 각 실린더의 'rodLocalZ_Retracted' 값을
        // 현재 로컬 위치로 자동 저장하는 옵션 (선택 사항, 인스펙터 수동 입력 권장)
        // if (boomCylinder.pistonRod != null) boomCylinder.rodLocalZ_Retracted = boomCylinder.pistonRod.localPosition.z;
        // if (armCylinder.pistonRod != null) armCylinder.rodLocalZ_Retracted = armCylinder.pistonRod.localPosition.z;
        // if (bucketCylinder.pistonRod != null) bucketCylinder.rodLocalZ_Retracted = bucketCylinder.pistonRod.localPosition.z;
    }

    void Update()
    {
        // 굴착기 암 입력 처리
        HandleInput();
        // 굴착기 암 회전 적용
        ApplyRotation();
    }

    void HandleInput()
    {
        float dt = Time.deltaTime;

        // 스윙 (Q / E)
        if (VirtualInput.inputs[(int)EINPUT.Q]) swingAngle -= swingSpeed * dt;
        if (VirtualInput.inputs[(int)EINPUT.E]) swingAngle += swingSpeed * dt;
        swingAngle = Mathf.Clamp(swingAngle, minSwingAngle, maxSwingAngle);

        // 붐 (W / S)
        if (VirtualInput.inputs[(int)EINPUT.W]) boomAngle += boomSpeed * dt;
        if (VirtualInput.inputs[(int)EINPUT.S]) boomAngle -= boomSpeed * dt;
        boomAngle = Mathf.Clamp(boomAngle, minBoomAngle, maxBoomAngle);

        // 암 (A / D)
        if (VirtualInput.inputs[(int)EINPUT.A]) armAngle += armSpeed * dt;
        if (VirtualInput.inputs[(int)EINPUT.D]) armAngle -= armSpeed * dt;
        armAngle = Mathf.Clamp(armAngle, minArmAngle, maxArmAngle);

        // 버킷 (R / F)
        if (VirtualInput.inputs[(int)EINPUT.R]) bucketAngle += bucketSpeed * dt;
        if (VirtualInput.inputs[(int)EINPUT.F]) bucketAngle -= bucketSpeed * dt;
        bucketAngle = Mathf.Clamp(bucketAngle, minBucketAngle, maxBucketAngle);
    }

    void ApplyRotation()
    {
        // 각 부품에 계산된 회전 각도 적용 (초기 로컬 회전을 기준으로)
        // Swing: Z축 (Vector3.forward) 기준 회전
        swing.localRotation = initSwingLocalRot * Quaternion.AngleAxis(swingAngle, Vector3.forward);

        // Boom: X축 (Vector3.right) 기준 회전
        // 이 부분에서 비틀림이 있었다면, `Vector3.` 부분을 `Vector3.right`로 수정하면 됩니다.
        boom.localRotation = initBoomLocalRot * Quaternion.AngleAxis(boomAngle, Vector3.right);

        // Arm: Y축 (Vector3.up) 기준 회전
        arm.localRotation = initArmLocalRot * Quaternion.AngleAxis(armAngle, Vector3.right);

        // Bucket: Y축 (Vector3.up) 기준 회전
        bucket.localRotation = initBucketLocalRot * Quaternion.AngleAxis(bucketAngle, Vector3.right);
    }
}