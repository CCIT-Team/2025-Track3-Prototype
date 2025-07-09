using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SoilParticle : MonoBehaviour
{
    [HideInInspector]
    public float m_fMass = 0.1f; // 입자 하나의 질량

    private Rigidbody m_Rigidbody;

    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.mass = m_fMass;
    }

    public bool IsAtRest()
    {
        // 속력이 아주 낮으면 정지 상태로 판단
        if (m_Rigidbody.velocity.sqrMagnitude < 0.01f && !m_Rigidbody.IsSleeping())
        {
            return true;
        }
        return false;
    }
}