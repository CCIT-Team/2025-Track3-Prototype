using UnityEngine;

// �� ��ũ��Ʈ�� Unity �������� 'Assets > Create' �޴��� ��Ÿ���� ���ִ� �ڵ��Դϴ�.
// menuName�� "Terrain/Soil Properties"�� �����߱� ������, Create �޴��� Terrain ������ ǥ�õ˴ϴ�.
[CreateAssetMenu(fileName = "NewSoil", menuName = "Terrain/Soil Properties")]
public class SoilProperties : ScriptableObject
{
    [Header("��� �⺻ ����")]

    [Tooltip("���� ������ (����: ��). �� ���ڰ� ���� ��Ƽ�� �����Դϴ�.")]
    public float m_fInternalFrictionAngle = 35.0f;

    [Tooltip("������ (����: kPa). �� ���ڰ� ���� �޶�ٴ� ���Դϴ�.")]
    public float m_fCohesion = 2.1f;

    [Tooltip("���� �е� (����: kg/m^3). ���� ������ ������ ��Ÿ���ϴ�.")]
    public float m_fBulkDensity = 1474.0f;

    [Header("�ùķ��̼ǿ� �Ķ����")]

    [Tooltip("���� ���������� �� ���ǰ� �þ�� �����Դϴ�.")]
    public float m_fSwellFactor = 1.25f;

    [Tooltip("���� ���� ���� ����Դϴ�. �������� �� �̲������ϴ�.")]
    public float m_fParticleFriction = 0.4f;

    [Tooltip("���� ���� �������Դϴ�. �������� ���ڵ��� ���� �������� �մϴ�.")]
    public float m_fParticleCohesion = 2.5f;
}