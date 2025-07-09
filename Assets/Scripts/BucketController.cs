using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BucketController : MonoBehaviour
{
    [Header("움직임 설정")]
    public float MoveForce = 100f;

    [Header("파기 설정")]
    public float DigStrength = 0.001f;
    public float DigRadius = 1.5f;
    public float MinSpeedToDig = 0.1f;
    public string GroundTag = "Terrain";

    [Header("쿨다운 설정")]
    public float DigCooldown = 0.1f;
    private float lastDigTime;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastDigTime = -DigCooldown;
    }

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.UpArrow)) rb.AddForce(Vector3.forward * MoveForce);
        if (Input.GetKey(KeyCode.DownArrow)) rb.AddForce(Vector3.back * MoveForce);
        if (Input.GetKey(KeyCode.LeftArrow)) rb.AddForce(Vector3.left * MoveForce);
        if (Input.GetKey(KeyCode.RightArrow)) rb.AddForce(Vector3.right * MoveForce);
        if (Input.GetKey(KeyCode.Space)) rb.AddForce(Vector3.up * MoveForce);
    }

    void OnCollisionStay(Collision collision)
    {
        if (Time.time < lastDigTime + DigCooldown) return;

        if (collision.gameObject.CompareTag(GroundTag) && rb.velocity.magnitude > MinSpeedToDig)
        {
            ContactPoint contact = collision.contacts[0];
            float digAmount = DigStrength * rb.velocity.magnitude;

            TerrainManager.Instance.Dig(contact.point, DigRadius, digAmount);

            lastDigTime = Time.time;
        }
    }
}