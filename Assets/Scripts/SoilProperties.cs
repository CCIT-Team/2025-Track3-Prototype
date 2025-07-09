using UnityEngine;

// 이 스크립트가 Unity 에디터의 'Assets > Create' 메뉴에 나타나게 해주는 코드입니다.
// menuName을 "Terrain/Soil Properties"로 설정했기 때문에, Create 메뉴의 Terrain 하위에 표시됩니다.
[CreateAssetMenu(fileName = "NewSoil", menuName = "Terrain/Soil Properties")]
public class SoilProperties : ScriptableObject
{
    [Header("토양 기본 정보")]

    [Tooltip("내부 마찰각 (단위: 도). 흙 입자가 서로 버티는 각도입니다.")]
    public float m_fInternalFrictionAngle = 35.0f;

    [Tooltip("점착력 (단위: kPa). 흙 입자가 서로 달라붙는 힘입니다.")]
    public float m_fCohesion = 2.1f;

    [Tooltip("용적 밀도 (단위: kg/m^3). 흙의 빽빽한 정도를 나타냅니다.")]
    public float m_fBulkDensity = 1474.0f;

    [Header("시뮬레이션용 파라미터")]

    [Tooltip("흙이 파헤쳐졌을 때 부피가 늘어나는 비율입니다.")]
    public float m_fSwellFactor = 1.25f;

    [Tooltip("입자 간의 마찰 계수입니다. 높을수록 덜 미끄러집니다.")]
    public float m_fParticleFriction = 0.4f;

    [Tooltip("입자 간의 점착력입니다. 높을수록 입자들이 서로 붙으려고 합니다.")]
    public float m_fParticleCohesion = 2.5f;
}