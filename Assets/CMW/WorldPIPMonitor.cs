using UnityEngine;

/// 월드 모니터에 버킷 서브 카메라를 표시 (런타임 RT 생성, 안전한 머티리얼 관리)
[DisallowMultipleComponent]
public class WorldPIPMonitor : MonoBehaviour
{
    [Header("Sources")]
    public Camera sourceCam;                 // 버킷 쪽 서브 카메라

    [Header("Monitor Mesh")]
    public MeshRenderer monitorRenderer;     // Quad 등
    [Tooltip("비워두면 Renderer의 머티리얼을 복제 or Unlit 머티리얼 생성")]
    public Material monitorMaterial;

    [Header("Render Texture")]
    public int width = 1024;
    public int height = 576;
    public int depthBuffer = 16;
    public int msaa = 1;                     // Quest: 1~2 권장
    public bool useMipMaps = false;

    [Header("Camera Settings")]
    public CameraClearFlags clearFlags = CameraClearFlags.SolidColor;
    public Color clearColor = Color.black;
    public bool allowHDR = false;

    [Header("Runtime Control")]
    public bool onlyRenderWhenVisible = true;
    public float visibilityGrace = 0.2f;
    public bool enableOnStart = true;

    [Header("Material Slot (고급)")]
    public string overrideTextureProperty = ""; // 비우면 자동(_BaseMap or _MainTex)

    RenderTexture _rt;
    Material _matInstance;
    int _texPropId = -1;
    float _visibleTimer;
    bool _wasVisible;

    // 생성물 플래그(에셋 파괴 방지)
    bool _createdRt;
    bool _createdMat; // 우리가 런타임에 만든/복제한 머티리얼만 Destroy

    void OnEnable()
    {
        if (!sourceCam || !monitorRenderer)
        {
            Debug.LogWarning("[WorldPIPMonitor] sourceCam/monitorRenderer 미지정");
            return;
        }

        // 머티리얼 준비: 제공된 에셋이 있으면 '복제', 없으면 Renderer의 공유머티리얼 복제 또는 Unlit 생성
        if (monitorMaterial)
        {
            _matInstance = new Material(monitorMaterial); // 복제본
            _createdMat = true;
        }
        else
        {
            var shared = monitorRenderer.sharedMaterial;
            if (shared)
            {
                _matInstance = new Material(shared);      // 복제본
                _createdMat = true;
            }
            else
            {
                // Unlit/Texture 또는 URP Unlit
                Shader sh = Shader.Find("Unlit/Texture");
                if (!sh) sh = Shader.Find("Universal Render Pipeline/Unlit");
                _matInstance = new Material(sh);
                _createdMat = true;
            }
        }
        monitorRenderer.material = _matInstance;

        // 텍스처 속성 자동 감지
        if (!string.IsNullOrEmpty(overrideTextureProperty))
            _texPropId = Shader.PropertyToID(overrideTextureProperty);
        else
            _texPropId = _matInstance.HasProperty("_BaseMap") ? Shader.PropertyToID("_BaseMap")
                      : Shader.PropertyToID("_MainTex");

        CreateAndBindRT();

        // 소스 카메라 기본 세팅
        sourceCam.allowHDR = allowHDR;
        sourceCam.clearFlags = clearFlags;
        sourceCam.backgroundColor = clearColor;

        SetActive(enableOnStart);
        _visibleTimer = enableOnStart ? visibilityGrace : 0f;
        _wasVisible = enableOnStart;
    }

    void OnDisable()
    {
        SetActive(false);
        ReleaseRT();

        // 에셋 파괴 금지: 우리가 만든/복제한 경우에만 Destroy
        if (_createdMat && _matInstance)
        {
            Destroy(_matInstance);
        }
        _matInstance = null;
        _createdMat = false;
    }

    void Update()
    {
        if (!sourceCam || !monitorRenderer) return;

        if (onlyRenderWhenVisible)
        {
            bool visible = monitorRenderer.isVisible;
            _visibleTimer = visible ? visibilityGrace : Mathf.Max(0f, _visibleTimer - Time.unscaledDeltaTime);

            bool shouldEnable = _visibleTimer > 0f;
            if (shouldEnable != _wasVisible)
            {
                sourceCam.enabled = shouldEnable;
                _wasVisible = shouldEnable;
            }
        }
    }

    public void SetActive(bool on)
    {
        if (sourceCam) sourceCam.enabled = on;
        if (monitorRenderer) monitorRenderer.enabled = on;
    }

    public void SetResolution(int w, int h, int aa = -1)
    {
        width = Mathf.Max(16, w);
        height = Mathf.Max(16, h);
        if (aa > 0) msaa = aa;
        CreateAndBindRT();
    }

    void CreateAndBindRT()
    {
        ReleaseRT();

        _rt = new RenderTexture(width, height, depthBuffer)
        {
            name = "RT_WorldPIP",
            antiAliasing = Mathf.Max(1, msaa),
            useMipMap = useMipMaps,
            autoGenerateMips = false
        };
        sourceCam.targetTexture = _rt;
        _createdRt = true;

        if (_matInstance && _texPropId != -1)
            _matInstance.SetTexture(_texPropId, _rt);
    }

    void ReleaseRT()
    {
        if (sourceCam) sourceCam.targetTexture = null;
        if (_createdRt && _rt)
        {
            _rt.Release();
            Destroy(_rt);
        }
        _rt = null;
        _createdRt = false;
    }
}
