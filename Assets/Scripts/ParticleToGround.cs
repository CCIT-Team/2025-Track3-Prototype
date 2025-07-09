using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ParticleToGround : MonoBehaviour
{
    public string GroundTag = "Terrain";

    [Tooltip("�� �ӵ� ���Ϸ� �����̸� ������ ������ �����մϴ�.")]
    public float SleepVelocityThreshold = 0.01f;
    [Tooltip("���� ���¸� �󸶳� �����ؾ� ������ ������ (��)")]
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

        // Rigidbody�� �ӵ��� �ſ� ������, ���� ����ִٸ�
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
            // �����ڰ� ���� ��츦 ����� ���� �ڵ�
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