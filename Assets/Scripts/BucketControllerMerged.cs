using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BucketControllerMerged : MonoBehaviour
{
    [Header("Blade Settings")]
    [SerializeField] private Collider bladeCollider;
    [SerializeField] private Transform bladeTransform;

    [Header("Excavation Settings")]
    [SerializeField] private float excavateRate = 3f;
    [SerializeField] private float particlePerCubicM = 30f;

    [Header("Spawn Settings")]
    [SerializeField, Tooltip("레이어별로 사용할 soil particle 프리팹을 지정하세요")]
    private GameObject[] soilPrefabs;
    [SerializeField] private int maxParticlesPerFrame = 500;

    [Header("Depth & Layers")]
    [SerializeField] private float depthOffset = 0.1f;
    [SerializeField] private LayerMask terrainLayer;

    [Header("VFX Settings")]
    [SerializeField] private GameObject dustVFXPrefab;
    [SerializeField] private float vfxCooldown = 0.5f;

    [Header("References (Assign in Inspector)")]
    [SerializeField, Tooltip("하나라도 할당하세요")]
    private TerrainDeformManagerMerged deformManager;
    [SerializeField, Tooltip("하나라도 할당하세요")]
    private Terrain terrain;
    [SerializeField, Tooltip("PublicMerged 버전")]
    private ExcavatorController_publicMerged excavatorControllerPublic;
    [SerializeField, Tooltip("Poly 버전")]
    private ExcavatorController_Poly excavatorControllerPoly;
    [SerializeField, Tooltip("Transform 기반 Inertia 컨트롤러(선택)")]
    private ExcavatorControllerInertia excavatorControllerInertia; // ★ 추가

    // ------------------------ 추가: VFX/입자 민감도 & 폴백/최적화 ------------------------
    [Header("VFX Gate (실제 파기일 때만)")]
    [SerializeField, Tooltip("먼지 VFX가 나오기 위한 최소 침투 깊이(m)")]
    private float vfxMinPenetration = 0.02f;
    [SerializeField, Tooltip("버킷 '아래방향' 성분 속도 임계값(m/s)")]
    private float vfxMinNormalSpeed = 0.25f;
    private Vector3 _lastBladeCenter;
    private bool _lastBladeCenterValid = false;

    [Header("각도 무시 Dig 허용(실제 파고들면)")]
    [SerializeField] private bool forceDigWhenPenetrating = true;
    [SerializeField] private float forceDigPenetration = 0.015f;

    [Header("입자 스폰 최적화/폴백")]
    [SerializeField, Tooltip("프레임당 입자 생성 예산(스파이크 방지)")]
    private int particleSpawnBudgetPerFrame = 20;
    [SerializeField, Tooltip("soilPrefabs가 비었거나 null일 때 사용할 기본 프리팹")]
    private GameObject defaultSoilPrefab;
    private int _cachedDominantLayer = -1;
    private int _cachedLayerFrame = -1;

    [Header("변형 배칭(부하 감소)")]
    [SerializeField, Tooltip("터레인 변형 호출 주기(초). 0이면 매 FixedUpdate")]
    private float deformInterval = 1f / 30f;
    private float _deformTimer;

    // -----------------------------------------------------------------------------

    private BucketGrabberMultiMerged _modeCtrl;
    private Rigidbody _rb;
    private Collider _col;
    private TerrainCollider _terrainCollider;
    private float _particleAccumulator;
    private float nextVfxTime = 0f;

    public bool isDigging { get; private set; }

    [Header("Soil Particle Painting")]
    [SerializeField] private Material[] _mats;
    [SerializeField] private TerrainLayer _diggedLayerTexture;
    [SerializeField] private float _diggedLayerWeight = 1f;
    private List<Material> _buffer = new List<Material>();

    private GameObjectPool _pool;

    // 어느 컨트롤러로 동작할지
    private enum ControllerType { None, Inertia, PublicMerged, Poly } // ★ Inertia 추가
    private ControllerType _activeController = ControllerType.None;

    void Start()
    {
        // Rigidbody 세팅
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Collider 세팅
        _col = GetComponent<Collider>();

        // deformManager 체크
        if (deformManager == null)
        {
            Debug.LogError("[BucketControllerMerged] TerrainDeformManagerMerged 미할당!");
            enabled = false; return;
        }
        if (!deformManager.gameObject.activeInHierarchy)
            deformManager.gameObject.SetActive(true);

        // terrain 체크
        if (terrain == null)
        {
            Debug.LogError("[BucketControllerMerged] Terrain 미할당!");
            enabled = false; return;
        }
        _terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (_terrainCollider == null)
            _terrainCollider = deformManager.GetComponent<TerrainCollider>();

        // ExcavatorController 결정 (우선순위: Inertia > PublicMerged > Poly)
        if (excavatorControllerInertia != null)
            _activeController = ControllerType.Inertia;
        else if (excavatorControllerPublic != null)
            _activeController = ControllerType.PublicMerged;
        else if (excavatorControllerPoly != null)
            _activeController = ControllerType.Poly;
        else
        {
            Debug.LogWarning("[BucketControllerMerged] ExcavatorController 미할당. 모드 자동결정 없이 '침투 강제 Dig'만 사용합니다.");
            _activeController = ControllerType.None;
        }

        // Grabber 모드 컨트롤러
        _modeCtrl = GetComponentInParent<BucketGrabberMultiMerged>();

        _lastBladeCenterValid = false;
        _deformTimer = 0f;
    }

    void FixedUpdate()
    {
        // 모드/접지 판단
        UpdateMode();
        DetectDig();

        if (bladeCollider == null)
            return;

        Bounds bb = bladeCollider.bounds;

        // 침투량 계산(블레이드 아래 4코너 기준)
        Vector3 tPos = terrain.transform.position;
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(bb.min.x, bb.min.y, bb.min.z),
            new Vector3(bb.min.x, bb.min.y, bb.max.z),
            new Vector3(bb.max.x, bb.min.y, bb.min.z),
            new Vector3(bb.max.x, bb.min.y, bb.max.z)
        };
        float penetration = 0f;
        for (int i = 0; i < 4; i++)
        {
            float groundY = terrain.SampleHeight(corners[i]) + tPos.y;
            float p = groundY - corners[i].y;
            if (p > penetration) penetration = p;
        }
        penetration = Mathf.Max(0f, penetration);

        // 각도상 Dig가 아니어도 실제로 파고들면 Dig 허용
        bool modeAllowsDig = (_modeCtrl != null && _modeCtrl.CurrentMode == BucketGrabberMultiMerged.Mode.Dig);
        bool forceDig = forceDigWhenPenetrating && (penetration > forceDigPenetration);

        if (!isDigging || !(modeAllowsDig || forceDig))
            return;

        // 실제 터레인과 겹침 확인
        if (Physics.OverlapBox(bb.center, bb.extents, bladeCollider.transform.rotation, terrainLayer).Length == 0)
            return;

        // 변형 호출 배칭(부하 감소)
        _deformTimer += Time.fixedDeltaTime;
        if (deformInterval > 0f && _deformTimer < deformInterval)
            return;
        float deltaVol = excavateRate * (deformInterval > 0f ? _deformTimer : Time.fixedDeltaTime);
        _deformTimer = 0f;

        // 땅 파기 + 텍스처 페인트
        deformManager.LowerRectAABBAsync(bb.min, bb.max, deltaVol, penetration);
        if (_diggedLayerTexture != null)
            deformManager.PaintTexture(bb.min, bb.max, _diggedLayerTexture, _diggedLayerWeight);

        // Dust VFX: 실제 파기(침투 + 아래로 파고드는 속도)일 때만
        float normalSpeed = 0f;
        Vector3 centerNow = bb.center;
        if (_lastBladeCenterValid)
        {
            Vector3 v = (centerNow - _lastBladeCenter) / Mathf.Max(1e-6f, Time.fixedDeltaTime);
            Vector3 downN = -bladeCollider.transform.up.normalized;
            normalSpeed = Mathf.Max(0f, Vector3.Dot(v, downN)); // 아래로 파고드는 성분
        }
        _lastBladeCenter = centerNow;
        _lastBladeCenterValid = true;

        bool activeDigging = (penetration >= vfxMinPenetration) && (normalSpeed >= vfxMinNormalSpeed);
        if (dustVFXPrefab != null && activeDigging && Time.time >= nextVfxTime)
        {
            nextVfxTime = Time.time + vfxCooldown;
            Instantiate(dustVFXPrefab, bb.center, Quaternion.identity);
        }

        // 입자 스폰
        SpawnParticles(deltaVol, bb);
    }

    private void SpawnParticles(float carvedVol, Bounds bb)
    {
        if (carvedVol <= 0f || particlePerCubicM <= 0f)
            return;

        _particleAccumulator += carvedVol * particlePerCubicM;
        int want = Mathf.FloorToInt(_particleAccumulator);
        _particleAccumulator -= want;

        // 프레임당 예산/상한
        int toSpawn = Mathf.Min(want, Mathf.Min(maxParticlesPerFrame, particleSpawnBudgetPerFrame));
        if (toSpawn <= 0) return;

        // 프레임당 지형 레이어 1회만 샘플
        int dominantLayer = GetDominantLayerOncePerFrame(bb.center);

        // 프리팹 선택(안전 폴백)
        GameObject prefab = null;
        if (soilPrefabs != null && soilPrefabs.Length > 0)
        {
            int idx = Mathf.Clamp(dominantLayer, 0, soilPrefabs.Length - 1);
            prefab = soilPrefabs[idx];
            if (prefab == null) prefab = defaultSoilPrefab;
        }
        else
        {
            prefab = defaultSoilPrefab;
        }
        if (prefab == null) return;

        Vector3 tPos = terrain.transform.position;

        for (int i = 0; i < toSpawn; i++)
        {
            float x = Random.Range(bb.min.x, bb.max.x);
            float z = Random.Range(bb.min.z, bb.max.z);
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + tPos.y + 0.4f;

            var go = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity);

            // (Optional) TerrainLayer 정보 전달
            if (go.TryGetComponent<SoilParticleMerged>(out var p))
            {
                var layers = terrain.terrainData.terrainLayers;
                if (layers != null && layers.Length > 0)
                {
                    int safeLayer = Mathf.Clamp(dominantLayer, 0, layers.Length - 1);
                    p.SetLayer(layers[safeLayer]);
                }
            }
            /*
            if (go.TryGetComponent<Rigidbody>(out var rb))
                
                rb.mass = 0.1f;
            */

        }
    }

    private int GetDominantLayerOncePerFrame(Vector3 sampleWorldPos)
    {
        if (Time.frameCount == _cachedLayerFrame && _cachedDominantLayer >= 0)
            return _cachedDominantLayer;

        TerrainData tData = terrain.terrainData;
        Vector3 tPos = terrain.transform.position;
        int w = tData.alphamapWidth, h = tData.alphamapHeight;

        int mapX = Mathf.Clamp((int)((sampleWorldPos.x - tPos.x) / tData.size.x * w), 0, w - 1);
        int mapZ = Mathf.Clamp((int)((sampleWorldPos.z - tPos.z) / tData.size.z * h), 0, h - 1);

        float[,,] alphas = tData.GetAlphamaps(mapX, mapZ, 1, 1);
        int dominantLayer = 0; float maxMix = 0f;
        for (int layer = 0; layer < alphas.GetLength(2); layer++)
        {
            float v = alphas[0, 0, layer];
            if (v > maxMix) { maxMix = v; dominantLayer = layer; }
        }

        _cachedLayerFrame = Time.frameCount;
        _cachedDominantLayer = dominantLayer;
        return _cachedDominantLayer;
    }

    private void UpdateMode()
    {
        // 모드 컨트롤러가 없으면 스킵(침투 강제 로직만으로 작동)
        if (_modeCtrl == null) return;

        // 할당된 컨트롤러에서 bucketAngle 가져오기
        float angle = 0f;
        switch (_activeController)
        {
            case ControllerType.Inertia:
                if (excavatorControllerInertia != null)
                    angle = excavatorControllerInertia.bucketAngle;
                else
                    return;
                break;
            case ControllerType.PublicMerged:
                if (excavatorControllerPublic != null)
                    angle = excavatorControllerPublic.bucketAngle;
                else
                    return;
                break;
            case ControllerType.Poly:
                if (excavatorControllerPoly != null)
                    angle = excavatorControllerPoly.bucketAngle;
                else
                    return;
                break;
            case ControllerType.None:
                return;
        }

        // 모드 결정(간단 규칙)
        BucketGrabberMultiMerged.Mode desired;
        if (angle < 0f) desired = BucketGrabberMultiMerged.Mode.Dump;
        else if (angle < 45f) desired = BucketGrabberMultiMerged.Mode.Dig;
        else desired = BucketGrabberMultiMerged.Mode.Idle;

        if (_modeCtrl.CurrentMode != desired)
            _modeCtrl.SetMode(desired);
    }

    private void DetectDig()
    {
        if (bladeCollider == null || terrain == null) { isDigging = false; return; }

        float bladeY = bladeCollider.bounds.min.y;
        float groundY = terrain.SampleHeight(bladeTransform.position) + terrain.transform.position.y;
        isDigging = (groundY - bladeY) > depthOffset;

        if (_col != null && _terrainCollider != null)
            Physics.IgnoreCollision(_col, _terrainCollider, isDigging);
    }
}
