using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ParticleToGround : MonoBehaviour
{
    public string GroundTag = "Terrain";

    [Tooltip("이 속도 이하로 움직이면 정지한 것으로 간주합니다.")]
    public float SleepVelocityThreshold = 0.01f;
    [Tooltip("정지 상태를 얼마나 유지해야 땅으로 변할지 (초)")]
    public float TimeToSolidify = 0.5f;

    private Rigidbody rb;
    private float sleepTimer = 0f;
    private bool isTouchingGround = false;
    private bool isRegistered = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (isRegistered) return;

        // Rigidbody의 속도가 매우 느리고, 땅에 닿아있다면
        if (rb.velocity.magnitude < SleepVelocityThreshold && isTouchingGround)
        {
            sleepTimer += Time.deltaTime;
        }
        else
        {
            sleepTimer = 0f;
        }

        if (sleepTimer >= TimeToSolidify)
        {
            Solidify();
        }
    }

    private void Solidify()
    {
        isRegistered = true;
        if (ParticleToGroundManager.Instance != null)
        {
            ParticleToGroundManager.Instance.RegisterParticle(this);
        }
        else
        {
            // 관리자가 없는 경우를 대비한 안전 코드
            Destroy(gameObject);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(GroundTag))
        {
            isTouchingGround = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(GroundTag))
        {
            isTouchingGround = false;
        }
    }
}