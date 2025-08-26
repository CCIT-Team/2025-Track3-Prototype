using UnityEngine;
using UnityEngine.UI;

/// 버킷 내부 질량/개수를 보기 좋게 표시
/// - uiText를 연결하면 해당 Text에 출력
/// - uiText가 없으면 화면 좌상단 OnGUI로 출력(디버그용)
public class BucketLoadReadout : MonoBehaviour
{
    [Header("Sensor")]
    public BucketLoadSensorTrigger sensor;

    [Header("UI (optional)")]
    public Text uiText;                         // Unity UI Text (없으면 OnGUI로 표시)
    [Tooltip("표시 포맷. {0}=질량 문자열, {1}=개수")]
    public string format = "Load: {0}  ({1}개)";

    [Header("Smoothing")]
    [Tooltip("표시값 스무딩 시간(초). 0이면 즉시 반영")]
    public float smoothTime = 0.15f;

    [Header("Auto Unit")]
    [Tooltip("임계치 이상이면 t(톤)으로 표기")]
    public bool autoUnit = true;
    public float tonThresholdKg = 1000f;

    [Header("Color Thresholds (optional)")]
    [Tooltip("이상일 때 경고색(soft)")]
    public float softKg = 350f;
    [Tooltip("이상일 때 경고색(hard)")]
    public float hardKg = 650f;
    public Color normalColor = Color.white;
    public Color softColor = new Color(1f, 0.85f, 0.2f); // 노랑
    public Color hardColor = new Color(1f, 0.3f, 0.3f);  // 빨강

    // 내부 상태
    float _displayKg;
    float _vel;

    void Reset()
    {
        // 에디터에서 자동 연결 시도
        if (!sensor) sensor = GetComponentInChildren<BucketLoadSensorTrigger>();
        if (!uiText)
        {
            // 씬에 Canvas/Text가 있다면 수동 연결 권장
        }
    }

    void Update()
    {
        if (!sensor) return;

        float target = Mathf.Max(0f, sensor.totalMass);
        if (smoothTime > 0f)
            _displayKg = Mathf.SmoothDamp(_displayKg, target, ref _vel, smoothTime);
        else
            _displayKg = target;

        string massStr = FormatMass(_displayKg);
        string msg = string.Format(format, massStr, sensor.countInside);

        if (uiText)
        {
            uiText.text = msg;
            uiText.color = PickColor(_displayKg);
        }
        else
        {
            // OnGUI 출력용 버퍼 저장
            _cachedMsg = msg;
            _cachedColor = PickColor(_displayKg);
        }
    }

    string FormatMass(float kg)
    {
        if (autoUnit && kg >= tonThresholdKg)
            return $"{kg / 1000f:0.0} t";
        if (kg >= 100f)
            return $"{kg:0} kg";
        return $"{kg:0.0} kg";
    }

    Color PickColor(float kg)
    {
        if (kg >= hardKg) return hardColor;
        if (kg >= softKg) return softColor;
        return normalColor;
    }

    // --- 디버그용 OnGUI ---
    string _cachedMsg;
    Color _cachedColor = Color.white;

    void OnGUI()
    {
        if (uiText) return; // UI 텍스트가 있으면 OnGUI는 사용 안 함
        if (string.IsNullOrEmpty(_cachedMsg)) return;

        var prevColor = GUI.color;
        GUI.color = _cachedColor;

        // 간단한 박스 스타일
        GUILayout.BeginArea(new Rect(10, 10, 320, 36), GUI.skin.box);
        GUILayout.Label(_cachedMsg, GUI.skin.label);
        GUILayout.EndArea();

        GUI.color = prevColor;
    }
}
