using UnityEngine;
using UnityEngine.LowLevel;

/// ExcavatorAudioController (개선판)
/// - 엔진 루프: 항상 재생(기본 볼륨 + 움직임/적재 가중)
/// - 유압 루프: 관절 각속도에 비례해 볼륨/피치
/// - 굴착 루프: 블레이드가 지면/토사에 닿아 있고, (옵션) 아래로 움직일 때만 재생
/// - 에디터 편의: 리스너와 멀면 자동 2D로 전환(옵션)

public enum ELOOPTYPE { None = -1, Engine, Move, Dig, Count }

[DisallowMultipleComponent]
public class ExcavatorAudioController : MonoBehaviour
{


    [Header("Refs (Transforms)")]
    [SerializeField] Transform swing;
    [SerializeField] Transform boom;
    [SerializeField] Transform arm;
    [SerializeField] Transform bucket;

    [Header("Blade / Contact")]
    [SerializeField] BoxCollider bladeBox;                 // 버킷 날(Box/Convex)
    [SerializeField] LayerMask soilAndTerrainMask = ~0;    // 토사/지면 레이어
    [SerializeField] BucketLoadSensorTrigger loadSensor;   // 버킷 내부 질량(kg)

    [Header("Audio Sources (Loop)")]
    [SerializeField] AudioSource engineLoop;               // 엔진
    [SerializeField] AudioSource moveLoop;                 // 유압/움직임
    [SerializeField] AudioSource digLoop;                  // 굴착(흙 긁는 소리)

    [Header("Loop Clips")]
    [SerializeField] AudioClip engineClip;
    [SerializeField] AudioClip moveClip;
    [SerializeField] AudioClip digClip;


    //[Header("One-shot (optional)")]
    //[SerializeField] AudioSource oneShot;
    //[SerializeField] AudioClip[] digImpacts;

    //[Header("Engine Tuning")]
    //[SerializeField] float enginePitchMin = 0.85f;
    //[SerializeField] float enginePitchMax = 1.45f;
    //[Tooltip("엔진 기본 볼륨(항상 깔림)")]
    //[SerializeField] float engineBaseVol = 0.35f;
    //[Tooltip("최대 볼륨(기본+가중 합)")]
    //[SerializeField] float engineMaxVol = 0.9f;
    //[SerializeField] float engineResponse = 0.25f;
    //[Tooltip("엔진 로드 = 움직임*W1 + 적재*W2")]
    //[SerializeField] float wMoveToEngine = 0.6f;
    //[SerializeField] float wLoadToEngine = 0.5f;
    //[SerializeField] float loadRefKg = 350f;

    //[Header("Movement(Hydraulics) Tuning")]
    //[SerializeField] float boomMaxDps = 60f, armMaxDps = 80f, bucketMaxDps = 90f, swingMaxDps = 60f;
    //[SerializeField] float wBoom = 1f, wArm = 1f, wBucket = 1f, wSwing = 0.6f;
    //[SerializeField] float moveVolMax = 0.85f;
    //[SerializeField] float movePitchMin = 0.9f, movePitchMax = 1.2f;
    //[SerializeField] float moveResponse = 0.15f;

    //[Header("Digging Tuning")]
    //[SerializeField] float digCheckPad = 0.02f;           // OverlapBox 축소치
    //[Tooltip("아래로만 체크하려면 아래 옵션 On")]
    //[SerializeField] float digSpeedThresh = 0.15f;        // 방향 무관 속도 임계
    //[SerializeField] float digVolMax = 1.0f;
    //[SerializeField] float digPitchMin = 0.85f, digPitchMax = 1.2f;
    //[SerializeField] float digEnterDelay = 0.05f, digExitDelay = 0.10f;
    //[SerializeField] float digResponse = 0.1f;

    //[Header("Downward-only Digging")]
    //[Tooltip("체크 시, 블레이드가 '아래로' 움직일 때만 굴착 사운드")]
    //[SerializeField] bool digUseDownwardOnly = true;
    //[Tooltip("아래로 속도 임계(m/s)")]
    //[SerializeField] float downSpeedThresh = 0.12f;

    //[Header("Debug Audibility (Editor helper)")]
    //[Tooltip("리스너와 멀면 자동 2D로 전환하여 항상 들리게(테스트용)")]
    //[SerializeField] bool alwaysHearInEditor = true;
    //[SerializeField] float hear2DThreshold = 80f;

    // 내부 상태
    Quaternion _prevSwing, _prevBoom, _prevArm, _prevBucket;
    Vector3 _prevBladePos;
    float _engineT, _moveT, _digT, _vEngine, _vMove, _vDig;
    float _digTimer; bool _digLatched;
    float _lastImpactTime;

    AudioListener _listener;
    float _listenerRefreshTimer;

    private void Awake()
    {
        _listener = Camera.main.GetComponent<AudioListener>();
        if(engineLoop == null)
        {
            engineLoop = gameObject.AddComponent<AudioSource>();
            engineLoop.playOnAwake = false;
            engineLoop.loop = true;
        }
        if (moveLoop == null)
        {
            moveLoop = gameObject.AddComponent<AudioSource>();
            moveLoop.playOnAwake = false;
            moveLoop.loop = true;
        }
        if (digLoop == null)
        {
            digLoop = gameObject.AddComponent<AudioSource>();
            digLoop.playOnAwake = false;
            digLoop.loop = true;
        }
    }

    void Start()
    {
        engineLoop.clip = engineClip;
        moveLoop.clip = moveClip;
        digLoop.clip = digClip;

        if (swing) _prevSwing = swing.localRotation;
        if (boom) _prevBoom = boom.localRotation;
        if (arm) _prevArm = arm.localRotation;
        if (bucket) _prevBucket = bucket.localRotation;
        if (bladeBox) _prevBladePos = bladeBox.transform.position;

        ToggleLoop(ELOOPTYPE.Engine, true);
    }

    void Update()
    {
        //float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        //// --- 블레이드 속도(버그픽스: 한 번만 Δ 계산) ---
        //Vector3 curBladePos = bladeBox ? bladeBox.transform.position : _prevBladePos;
        //Vector3 bladeDelta = curBladePos - _prevBladePos;
        //float bladeSpeed = bladeDelta.magnitude / dt;
        //float bladeDownSpeed = Mathf.Max(0f, -bladeDelta.y) / dt; // 아래로만 양수
        //_prevBladePos = curBladePos;

        //// --- 관절 각속도 → 유압 활동도 ---
        //float dpsBoom = GetAngularSpeed(boom, ref _prevBoom, dt);
        //float dpsArm = GetAngularSpeed(arm, ref _prevArm, dt);
        //float dpsBucket = GetAngularSpeed(bucket, ref _prevBucket, dt);
        //float dpsSwing = GetAngularSpeed(swing, ref _prevSwing, dt);

        //float nBoom = Mathf.Clamp01(dpsBoom / Mathf.Max(1f, boomMaxDps));
        //float nArm = Mathf.Clamp01(dpsArm / Mathf.Max(1f, armMaxDps));
        //float nBucket = Mathf.Clamp01(dpsBucket / Mathf.Max(1f, bucketMaxDps));
        //float nSwing = Mathf.Clamp01(dpsSwing / Mathf.Max(1f, swingMaxDps));

        //float moveActivity = Mathf.Clamp01(nBoom * wBoom + nArm * wArm + nBucket * wBucket + nSwing * wSwing);

        //// --- 굴착 판단 ---
        //bool contact = BladeTouchesSoil();
        //bool diggingNow = digUseDownwardOnly
        //    ? (contact && bladeDownSpeed >= Mathf.Max(0.0001f, downSpeedThresh))
        //    : (contact && bladeSpeed >= Mathf.Max(0.0001f, digSpeedThresh));

        //if (diggingNow) _digTimer = Mathf.Min(digEnterDelay, _digTimer + dt);
        //else _digTimer = Mathf.Max(-digExitDelay, _digTimer - dt);

        //if (!_digLatched && _digTimer >= digEnterDelay) { _digLatched = true; TryImpact(); }
        //if (_digLatched && _digTimer <= -digExitDelay) { _digLatched = false; }

        //// --- 엔진 로드 계산(움직임+적재) ---
        //float loadKg = (loadSensor ? Mathf.Max(0f, loadSensor.totalMass) : 0f);
        //float loadNorm = (loadRefKg > 0f) ? (loadKg / (loadKg + loadRefKg)) : 0f;
        //float engineLoad = Mathf.Clamp01(moveActivity * wMoveToEngine + loadNorm * wLoadToEngine);

        //// --- 스무딩 ---
        //_engineT = Mathf.SmoothDamp(_engineT, engineLoad, ref _vEngine, Mathf.Max(0.01f, engineResponse));
        //_moveT = Mathf.SmoothDamp(_moveT, moveActivity, ref _vMove, Mathf.Max(0.01f, moveResponse));

        //float digTarget = 0f;
        //if (_digLatched)
        //{
        //    if (digUseDownwardOnly)
        //        digTarget = Mathf.Clamp01(Mathf.InverseLerp(downSpeedThresh, downSpeedThresh * 3f, bladeDownSpeed));
        //    else
        //        digTarget = Mathf.Clamp01(Mathf.InverseLerp(digSpeedThresh, digSpeedThresh * 3f, bladeSpeed));
        //}
        //_digT = Mathf.SmoothDamp(_digT, digTarget, ref _vDig, Mathf.Max(0.01f, digResponse));

        if (VirtualInput.inputs[(int)EINPUT.WheelRF] || VirtualInput.inputs[(int)EINPUT.WheelLF] || VirtualInput.inputs[(int)EINPUT.WheelRB] ||VirtualInput.inputs[(int)EINPUT.WheelLB])
            if(!moveLoop.isPlaying)
                ToggleLoop(ELOOPTYPE.Move, true);
        else
            ToggleLoop(ELOOPTYPE.Move, false);
        //if (digLoop && digLoop.clip)
        //{
        //    digLoop.volume = Mathf.Lerp(0f, digVolMax, _digT);
        //    digLoop.pitch = Mathf.Lerp(digPitchMin, digPitchMax, _digT);
        //    TogglePlayByVolume(digLoop);
        //}
    }

    void LateUpdate()
    {
        //if (!alwaysHearInEditor) return;

        //void SetBlendByDist(AudioSource s)
        //{
        //    if (!s) return;
        //    float d = Vector3.Distance(_listener.transform.position, s.transform.position);
        //    s.spatialBlend = (d > hear2DThreshold) ? 0f : 1f; // 멀면 2D
        //    s.dopplerLevel = 0f;
        //}

        //SetBlendByDist(engineLoop);
        //SetBlendByDist(moveLoop);
        //SetBlendByDist(digLoop);
    }

    // --- Utils ---
    float GetAngularSpeed(Transform t, ref Quaternion prevLocal, float dt)
    {
        if (!t) return 0f;
        Quaternion cur = t.localRotation;
        float deg = Quaternion.Angle(prevLocal, cur);
        prevLocal = cur;
        return deg / Mathf.Max(1e-5f, dt);
    }

    //bool BladeTouchesSoil()
    //{
    //    if (!bladeBox) return false;

    //    Bounds bb = bladeBox.bounds;
    //    Vector3 ext = bb.extents - new Vector3(digCheckPad, digCheckPad, digCheckPad);
    //    if (ext.x <= 0f || ext.y <= 0f || ext.z <= 0f) return false;

    //    Collider[] hits = Physics.OverlapBox(
    //        bb.center, ext, bladeBox.transform.rotation,
    //        soilAndTerrainMask, QueryTriggerInteraction.Ignore
    //    );

    //    if (hits.Length == 0) return false;

    //    Transform root = (swing ? swing.root : transform.root);
    //    for (int i = 0; i < hits.Length; i++)
    //    {
    //        var h = hits[i];
    //        if (!h) continue;
    //        if (root && h.transform.IsChildOf(root)) continue; // 자기 파츠 제외
    //        return true;
    //    }
    //    return false;
    //}

    void TogglePlayByVolume(AudioSource s)
    {
        if (!s || !s.clip) return;
        if (s.volume > 0.01f) { if (!s.isPlaying) s.Play(); }
        else { if (s.isPlaying) s.Pause(); }
    }

    //void TryImpact()
    //{
    //    if (!oneShot || digImpacts == null || digImpacts.Length == 0) return;
    //    if (Time.time - _lastImpactTime < 0.15f) return; // 스팸 방지
    //    _lastImpactTime = Time.time;
    //    var clip = digImpacts[Random.Range(0, digImpacts.Length)];
    //    oneShot.PlayOneShot(clip, Mathf.Lerp(0.4f, 1f, _digT));
    //}

    public void ToggleLoop(ELOOPTYPE loopType, bool isPlay)
    {
        switch (loopType)
        {
            case ELOOPTYPE.Engine:
                if (isPlay)
                    engineLoop.Play();
                else
                    engineLoop.Stop();
                break;
            case ELOOPTYPE.Move:
                if (isPlay)
                    moveLoop.Play();
                else
                    moveLoop.Stop();
                break;
            case ELOOPTYPE.Dig:
                if (isPlay)
                    digLoop.Play();
                else
                    digLoop.Stop();
                break;
            default:
                break;
        }
    }
}
