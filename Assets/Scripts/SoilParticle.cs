using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SoilParticle : MonoBehaviour
{
    [HideInInspector]
    public float m_fMass = 0.1f; // ���� �ϳ��� ����

    private Rigidbody m_Rigidbody;

    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.mass = m_fMass;
    }

    public bool IsAtRest()
    {
        // �ӷ��� ���� ������ ���� ���·� �Ǵ�
        if (m_Rigidbody.velocity.sqrMagnitude < 0.01f && !m_Rigidbody.IsSleeping())
        {
            return true;
        }
        return false;
    }
}