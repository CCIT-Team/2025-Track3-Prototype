using UnityEngine;

public class ExcavatorControllerInertia : MonoBehaviour
{
    // ===== 입력 소스 =====
    public enum InputMode { UnityKeys, VirtualInput }
    [Header("Input Source")]
    public InputMode inputMode = InputMode.UnityKeys;
    public KeyCode keyQ = KeyCode.Q, keyE = KeyCode.E;
    public KeyCode keyW = KeyCode.W, keyS = KeyCode.S;
    public KeyCode keyA = KeyCode.A, keyD = KeyCode.D;
    public KeyCode keyR = KeyCode.R, keyF = KeyCode.F;
    public int idxQ = 0, idxE = 1, idxW = 2, idxS = 3, idxA = 4, idxD = 5, idxR = 6, idxF = 7;

    // ===== 파트 =====
    [Header("Excavator Parts")]
    public Transform swing;
    public Transform boom;
    public Transform arm;
    public Transform bucket;

    // ===== 속도/가동범위 =====
    [Header("Rotation Settings (no-load deg/s)")]
    public float swingSpeed = 30f;
    public float boomSpeed = 30f;
    public float armSpeed = 30f;
    public float bucketSpeed = 30f;

    [Header("Rotation Angle Limits (deg)")]
    public float minSwingAngle = -135f, maxSwingAngle = 135f;
    public float minBoomAngle = -15f, maxBoomAngle = 30f;
    public float minArmAngle = 0f, maxArmAngle = 90f;
    public float minBucketAngle = -15f, maxBucketAngle = 45f;

    // ===== 관성/덜컹 =====
    [Header("Inertia / Damping")]
    public float moveSmoothTime = 0.18f;
    public float holdSmoothTime = 0.12f;
    [Tooltip("손 뗄 때 속도 임펄스(덜컹)")]
    public float stopKickDegPerSec = 95f;
    [Tooltip("손 뗄 때 목표각 오버슈트(도)")]
    public float stopBumpDeg = 1.8f;
    [Tooltip("덜컹 감쇠(0~1, 클수록 빨리 죽음)")]
    public float stopBumpDamping = 0.45f;

    // ===== 무게/푸시 영향 =====
    [Header("Load & Push (sensors)")]
    public BucketLoadSensorTrigger loadSensor;   // 버킷 내부 적재(Trigger)
    public BucketPushSensorTrigger pushSensor;   // 버킷 앞 푸시(Trigger)
    [Tooltip("푸시 질량을 유효 적재로 환산하는 계수")]
    public float pushMassFactor = 1.2f;
    [Tooltip("속도 감소 기준 질량(kg)")]
    public float loadRefKg = 250f;
    public float loadSlowK = 1.2f;
    [Tooltip("아무리 무거워도 최소 이 비율로는 움직이도록")]
    public float minSpeedScale = 0.35f;

    [Header("Limits (lift/push)")]
    [Tooltip("부드러운 리밋(초과 시 추가 감속)")]
    public float softLiftKg = 350f;
    [Tooltip("들어올리기 하드 차단 질량 (버킷 내부 질량 기준)")]
    public float hardLiftKg = 650f;
    [Tooltip("눌러 밀기 하드 차단 질량 (푸시 질량 기준)")]
    public float hardPushKg = 650f;

    // ===== 스타트 보정/데드존 =====
    [Header("Startup & Deadzone")]
    [Tooltip("시작 후 이 시간 동안 센서값 무시 + 자동 테어(영점) 캡처")]
    public float startupIgnoreTime = 0.5f;
    [Tooltip("이하 질량은 노이즈로 무시")]
    public float loadDeadzoneKg = 20f;
    [Tooltip("푸시 데드존(kg)")]
    public float pushDeadzoneKg = 25f;
    public bool autoTareOnStartup = true;

    // ===== 차체 앞쏠림 =====
    [Header("Base Tilt (forward pitch)")]
    [Tooltip("틸트는 기본적으로 리프트 질량에만 반응")]
    public bool tiltUseLiftOnly = true;
    [Tooltip("틸트 시작 최소 질량(kg)")]
    public float tiltMinKg = 60f;
    public float maxForwardTiltDeg = 6f;
    public float tiltSmoothTime = 0.25f;
    public Transform baseTilt; // 비우면 swing

    // ===== '물체-지면 압착' 가드 (Terrain 관통 허용, 물체만 보호) =====
    [Header("Crush Guard (object vs terrain)")]
    [Tooltip("버킷 날(BoxCollider) — 물체 윗면 접촉 판단")]
    public BoxCollider bladeBox;
    public Terrain terrain;
    public Transform excavatorRoot;
    public LayerMask crushMask = ~0;         // 검사 대상 레이어 (RB 필요)

    [Tooltip("블레이드와 물체 윗면 접촉으로 간주할 여유")]
    public float topTouchClearance = 0.04f;
    [Tooltip("물체 바닥이 지면에 받쳐졌다고 간주하는 여유")]
    public float groundSupportClearance = 0.05f;
    [Tooltip("블레이드 주변 OverlapBox 패딩")]
    public Vector3 crushBoxPadding = new Vector3(0.02f, 0.02f, 0.02f);

    // ===== '물체-지면 압착' 필터 파라미터 =====
    [Header("Crush Filter (robust)")]
    [Tooltip("블레이드 발자국 위 X×Z 샘플 수")]
    public int crushGridX = 3, crushGridZ = 2;
    [Tooltip("압착으로 인정할 최소 커버율(0~1)")]
    [Range(0.1f, 0.9f)] public float crushCoverRatio = 0.35f;
    [Tooltip("압착으로 인정할 최소 질량(kg)")]
    public float crushEnterMinKg = 25f;
    [Tooltip("압착 판정 진입 지연(초)")]
    public float crushEnterDebounce = 0.06f;
    [Tooltip("압착 판정 해제 지연(초)")]
    public float crushExitDebounce = 0.10f;
    [Tooltip("블레이드 발자국을 가장자리 노이즈 제거용으로 줄임(m)")]
    public float bladeFootprintShrink = 0.02f;

    // ----- 내부 상태 -----
    private float swingAngle, boomAngle, armAngle;
    public float bucketAngle;
    private float curSwing, curBoom, curArm, curBucket;
    private float velSwing, velBoom, velArm, velBucket;
    float lastDirBoom, lastDirArm, lastDirBucket;
    bool prevCmdBoom, prevCmdArm, prevCmdBucket;
    float tiltCur, tiltVel;

    // crush latch 상태
    bool _crushLatched;
    float _crushTimer; // + 진입 누적 / - 해제 누적

    // 입력 상태
    bool _cmdSwing, _cmdBoom, _cmdArm, _cmdBucket;

    // 스타트/영점
    float _tStart;
    float _tareLift, _tarePush;
    bool _tareLocked;

    // 초기 로컬 회전 저장
    private Quaternion initSwingLocalRot, initBoomLocalRot, initArmLocalRot, initBucketLocalRot;
    private Quaternion initBaseTiltRot;

    void Start()
    {
        initSwingLocalRot = swing ? swing.localRotation : Quaternion.identity;
        initBoomLocalRot = boom ? boom.localRotation : Quaternion.identity;
        initArmLocalRot = arm ? arm.localRotation : Quaternion.identity;
        initBucketLocalRot = bucket ? bucket.localRotation : Quaternion.identity;
        if (!baseTilt) baseTilt = swing;
        initBaseTiltRot = baseTilt ? baseTilt.localRotation : Quaternion.identity;
        if (!excavatorRoot) excavatorRoot = swing ? swing.root : transform;

        curSwing = curBoom = curArm = curBucket = 0f;
        swingAngle = boomAngle = armAngle = bucketAngle = 0f;
        ApplyRotation();

        _tStart = Time.time;
        _tareLift = _tarePush = 0f;
        _tareLocked = !autoTareOnStartup;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 0) 입력 의도
        bool wantDownBoom = GetKey(keyS, idxS);
        bool wantDownArm = GetKey(keyD, idxD);
        bool wantDownBucket = GetKey(keyF, idxF);
        bool wantDownAny = wantDownBoom || wantDownArm || wantDownBucket;
        bool wantUpAny = GetKey(keyW, idxW) || GetKey(keyA, idxA) || GetKey(keyR, idxR);

        // 1) 센서 + 스타트/데드존/테어
        float rawLift = (loadSensor ? Mathf.Max(0f, loadSensor.totalMass) : 0f);
        float rawPush = (pushSensor ? Mathf.Max(0f, pushSensor.effectiveMass) : 0f);
        bool inStartup = (Time.time - _tStart) < Mathf.Max(0f, startupIgnoreTime);

        if (autoTareOnStartup && !inStartup && !_tareLocked) _tareLocked = true;
        if (autoTareOnStartup && inStartup)
        {
            _tareLift = Mathf.Max(_tareLift, rawLift);
            _tarePush = Mathf.Max(_tarePush, rawPush);
        }

        float liftKg = Mathf.Max(0f, rawLift - _tareLift);
        float pushKg = Mathf.Max(0f, rawPush - _tarePush);
        if (inStartup) { liftKg = 0f; pushKg = 0f; }

        if (liftKg < loadDeadzoneKg) liftKg = 0f;
        if (pushKg < pushDeadzoneKg) pushKg = 0f;

        float pushEffKg = pushKg * Mathf.Max(0f, pushMassFactor);
        float totalKgForSpeed = liftKg + pushEffKg;

        // 2) 속도 스케일(느려지기는 그대로, 막힘은 방향별로 분리)
        float loadRatio = (loadRefKg <= 0f) ? 0f : totalKgForSpeed / (totalKgForSpeed + loadRefKg);
        float speedScale = 1f / (1f + loadSlowK * Mathf.Clamp01(loadRatio));
        if (totalKgForSpeed > softLiftKg) speedScale *= Mathf.Clamp01(softLiftKg / totalKgForSpeed);
        speedScale = Mathf.Clamp(speedScale, Mathf.Clamp01(minSpeedScale), 1f);

        float moveT = moveSmoothTime / Mathf.Max(0.2f, speedScale);
        float holdT = holdSmoothTime * (1f + 0.5f * loadRatio);

        // 3) 하드 리밋(방향 분리)
        // ↑ 들어올리기/말기 할 때는 버킷 내부 질량만 체크
        bool overHardLift = wantUpAny && (liftKg > hardLiftKg);
        // ↓ 내리기/펴기 할 때는 푸시 질량만 체크
        bool overHardPush = wantDownAny && (pushEffKg > hardPushKg);

        // 4) '물체-지면 압착' 감지 (필터/디바운스). Terrain 관통은 허용.
        bool crushingNow = false; float coverNow = 0f, massNow = 0f;
        if (wantDownAny) crushingNow = IsCrushingObjectAgainstGroundFiltered(out coverNow, out massNow);

        if (crushingNow && massNow >= crushEnterMinKg && coverNow >= crushCoverRatio)
            _crushTimer = Mathf.Min(crushEnterDebounce, _crushTimer + Time.deltaTime);
        else
            _crushTimer = Mathf.Max(-crushExitDebounce, _crushTimer - Time.deltaTime);

        if (!_crushLatched && _crushTimer >= crushEnterDebounce) _crushLatched = true;
        if (_crushLatched && _crushTimer <= -crushExitDebounce) _crushLatched = false;

        // ↓ 아래로 누를 때만 crush 차단을 적용 (반대방향은 자유롭게 풀림)
        bool crushing = _crushLatched && wantDownAny;

        // 5) 입력 처리
        HandleInput(dt, speedScale, overHardLift, overHardPush, crushing);

        // 6) 덜컹
        ApplyStopKickAndBump();

        // 7) 보간
        curSwing = SmoothTo(curSwing, swingAngle, ref velSwing, moveT, holdT, _cmdSwing);
        curBoom = SmoothTo(curBoom, boomAngle, ref velBoom, moveT, holdT, _cmdBoom);
        curArm = SmoothTo(curArm, armAngle, ref velArm, moveT, holdT, _cmdArm);
        curBucket = SmoothTo(curBucket, bucketAngle, ref velBucket, moveT, holdT, _cmdBucket);

        // 8) 앞쏠림
        float tiltMass = tiltUseLiftOnly ? liftKg : (liftKg + 0.5f * pushEffKg);
        float targetTilt = (tiltMass >= tiltMinKg)
            ? Mathf.Lerp(0f, maxForwardTiltDeg, Mathf.Clamp01(tiltMass / (tiltMass + loadRefKg)))
            : 0f;
        tiltCur = Mathf.SmoothDamp(tiltCur, targetTilt, ref tiltVel, tiltSmoothTime);

        // 9) 적용
        ApplyRotation();
    }

    // ===== 입력 처리 =====
    void HandleInput(float dt, float speedScale, bool overHardLift, bool overHardPush, bool crushing)
    {
        float sSwing = swingSpeed * speedScale;
        float sBoom = boomSpeed * speedScale;
        float sArm = armSpeed * speedScale;
        float sBucket = bucketSpeed * speedScale;

        _cmdSwing = _cmdBoom = _cmdArm = _cmdBucket = false;

        // 스윙 (Q/E)
        if (GetKey(keyQ, idxQ)) { swingAngle -= sSwing * dt; _cmdSwing = true; }
        if (GetKey(keyE, idxE)) { swingAngle += sSwing * dt; _cmdSwing = true; }
        swingAngle = Mathf.Clamp(swingAngle, minSwingAngle, maxSwingAngle);

        // 붐 (W/S)
        if (GetKey(keyW, idxW) && !overHardLift) { boomAngle += sBoom * dt; _cmdBoom = true; lastDirBoom = +1f; }
        if (GetKey(keyS, idxS) && !overHardPush && !crushing) { boomAngle -= sBoom * dt; _cmdBoom = true; lastDirBoom = -1f; }
        boomAngle = Mathf.Clamp(boomAngle, minBoomAngle, maxBoomAngle);

        // 암 (A/D)
        if (GetKey(keyA, idxA) && !overHardLift) { armAngle += sArm * dt; _cmdArm = true; lastDirArm = +1f; }
        if (GetKey(keyD, idxD) && !overHardPush && !crushing) { armAngle -= sArm * dt; _cmdArm = true; lastDirArm = -1f; }
        armAngle = Mathf.Clamp(armAngle, minArmAngle, maxArmAngle);

        // 버킷 (R/F)
        if (GetKey(keyR, idxR) && !overHardLift) { bucketAngle += sBucket * dt; _cmdBucket = true; lastDirBucket = +1f; }
        if (GetKey(keyF, idxF) && !overHardPush && !crushing) { bucketAngle -= sBucket * dt; _cmdBucket = true; lastDirBucket = -1f; }
        bucketAngle = Mathf.Clamp(bucketAngle, minBucketAngle, maxBucketAngle);

        // 압착 중엔 하강 타겟만 봉인 (상승/반대방향은 그대로 허용)
        if (crushing)
        {
            boomAngle = Mathf.Max(boomAngle, curBoom);
            armAngle = Mathf.Max(armAngle, curArm);
            bucketAngle = Mathf.Max(bucketAngle, curBucket);
        }
    }

    // 손 뗄 때 덜컹(임펄스 + 작은 바운스)
    void ApplyStopKickAndBump()
    {
        // 임펄스
        if (prevCmdBoom && !_cmdBoom) velBoom += stopKickDegPerSec * Mathf.Sign(lastDirBoom == 0f ? 1f : lastDirBoom);
        if (prevCmdArm && !_cmdArm) velArm += stopKickDegPerSec * Mathf.Sign(lastDirArm == 0f ? 1f : lastDirArm);
        if (prevCmdBucket && !_cmdBucket) velBucket += stopKickDegPerSec * Mathf.Sign(lastDirBucket == 0f ? 1f : lastDirBucket);

        // 바운스(목표 오버슈트)
        if (prevCmdBoom && !_cmdBoom) boomAngle += Mathf.Sign(lastDirBoom) * stopBumpDeg;
        if (prevCmdArm && !_cmdArm) armAngle += Mathf.Sign(lastDirArm) * stopBumpDeg;
        if (prevCmdBucket && !_cmdBucket) bucketAngle += Mathf.Sign(lastDirBucket) * stopBumpDeg;

        // 리밋
        boomAngle = Mathf.Clamp(boomAngle, minBoomAngle, maxBoomAngle);
        armAngle = Mathf.Clamp(armAngle, minArmAngle, maxArmAngle);
        bucketAngle = Mathf.Clamp(bucketAngle, minBucketAngle, maxBucketAngle);

        // 감쇠
        float d = 1f - Mathf.Clamp01(stopBumpDamping);
        velBoom *= d; velArm *= d; velBucket *= d;

        prevCmdBoom = _cmdBoom;
        prevCmdArm = _cmdArm;
        prevCmdBucket = _cmdBucket;
    }

    float SmoothTo(float cur, float target, ref float vel, float moveT, float holdT, bool commanding)
    {
        float smooth = commanding ? moveT : holdT;
        return Mathf.SmoothDampAngle(cur, target, ref vel, Mathf.Max(0.01f, smooth), Mathf.Infinity, Time.deltaTime);
    }

    void ApplyRotation()
    {
        if (swing)
        {
            Quaternion yawZ = Quaternion.AngleAxis(curSwing, Vector3.forward);
            Quaternion pitchX = Quaternion.AngleAxis(tiltCur, Vector3.right);
            swing.localRotation = initSwingLocalRot * pitchX * yawZ;
        }
        if (boom) boom.localRotation = initBoomLocalRot * Quaternion.AngleAxis(curBoom, Vector3.right);
        if (arm) arm.localRotation = initArmLocalRot * Quaternion.AngleAxis(curArm, Vector3.right);
        if (bucket) bucket.localRotation = initBucketLocalRot * Quaternion.AngleAxis(curBucket, Vector3.right);
    }

    // ===== '물체-지면 압착' 감지 (Terrain 관통 허용, 물체만 보호) =====
    bool IsCrushingObjectAgainstGroundFiltered(out float coverRatio, out float blockMassKg)
    {
        coverRatio = 0f;
        blockMassKg = 0f;

        if (!bladeBox) return false;

        Bounds bb = bladeBox.bounds;

        // 블레이드 발자국(바닥 XY)을 살짝 줄여 가장자리 노이즈 제거
        float minX = bb.min.x + bladeFootprintShrink;
        float maxX = bb.max.x - bladeFootprintShrink;
        float minZ = bb.min.z + bladeFootprintShrink;
        float maxZ = bb.max.z - bladeFootprintShrink;

        if (minX > maxX || minZ > maxZ) return false;

        int gx = Mathf.Max(1, crushGridX);
        int gz = Mathf.Max(1, crushGridZ);
        int totalSamples = gx * gz;

        // 샘플 고도: 블레이드 최저점 바로 위에서 살짝 아래로
        float yCastStart = bb.min.y + 0.01f;
        float castLen = 0.08f; // 윗면만 감지
        int layer = crushMask;

        System.Collections.Generic.Dictionary<Rigidbody, int> hitCounts = new();

        for (int ix = 0; ix < gx; ix++)
        {
            float tx = (gx == 1) ? 0.5f : (float)ix / (gx - 1);
            float x = Mathf.Lerp(minX, maxX, tx);

            for (int iz = 0; iz < gz; iz++)
            {
                float tz = (gz == 1) ? 0.5f : (float)iz / (gz - 1);
                float z = Mathf.Lerp(minZ, maxZ, tz);

                Vector3 origin = new Vector3(x, yCastStart, z);
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castLen, layer, QueryTriggerInteraction.Ignore))
                {
                    var rb = hit.rigidbody;
                    if (!rb) continue;
                    // 자기 파츠 제외
                    if (excavatorRoot && rb.transform.IsChildOf(excavatorRoot)) continue;

                    // 블레이드 바로 아래 "윗면"을 맞췄는지(블레이드와 가깝게)
                    float objTopY = hit.collider.bounds.max.y;
                    float distTop = bb.min.y - objTopY;
                    if (distTop > topTouchClearance * 2f) continue;

                    if (!hitCounts.ContainsKey(rb)) hitCounts[rb] = 0;
                    hitCounts[rb] += 1;
                }
            }
        }

        if (hitCounts.Count == 0) return false;

        // 가장 넓게 덮은 후보 선택
        Rigidbody best = null;
        int bestCount = 0;
        foreach (var kv in hitCounts)
        {
            if (kv.Value > bestCount) { best = kv.Key; bestCount = kv.Value; }
        }
        if (!best) return false;

        coverRatio = (float)bestCount / totalSamples;

        // 최소 커버율 미만이면 가벼운 스침 → 통과
        if (coverRatio < crushCoverRatio) return false;

        // 질량 기준
        blockMassKg = Mathf.Max(0f, best.mass);
        if (blockMassKg < crushEnterMinKg && !best.isKinematic) return false;

        // 물체가 Terrain에 받쳐져 있는지(최소 두 코너)
        bool onGround = false;
        if (terrain)
        {
            Collider bc = best.GetComponent<Collider>();
            if (!bc) bc = best.GetComponentInChildren<Collider>();
            if (!bc) return false;

            Bounds ob = bc.bounds;
            Vector3 tp = terrain.transform.position;

            Vector3[] basePts = new Vector3[]
            {
                new Vector3(ob.min.x, 0f, ob.min.z),
                new Vector3(ob.min.x, 0f, ob.max.z),
                new Vector3(ob.max.x, 0f, ob.min.z),
                new Vector3(ob.max.x, 0f, ob.max.z)
            };

            int nearCount = 0;
            for (int i = 0; i < basePts.Length; i++)
            {
                float ty = terrain.SampleHeight(basePts[i]) + tp.y;
                float gap = ob.min.y - ty;
                if (gap < groundSupportClearance * 3f) nearCount++;
            }
            onGround = nearCount >= 2;
        }
        else
        {
            onGround = true; // Terrain이 없으면 보수적으로 true
        }

        return onGround;
    }

    // ===== 유틸 =====
    bool GetKey(KeyCode kc, int idx)
    {
        if (inputMode == InputMode.UnityKeys) return Input.GetKey(kc);
        try
        {
            var type = System.Type.GetType("VirtualInput");
            if (type == null) return false;
            var field = type.GetField("inputs");
            if (field == null) return false;
            var arr = field.GetValue(null) as System.Array;
            if (arr == null || idx < 0 || idx >= arr.Length) return false;
            object val = arr.GetValue(idx);
            return val is bool b && b;
        }
        catch { return false; }
    }
}
