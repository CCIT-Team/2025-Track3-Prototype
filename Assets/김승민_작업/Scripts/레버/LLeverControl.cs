using UnityEngine;

public class LLeverControl : MonoBehaviour
{
    public Transform LLever;

    public float threshold = 10f;

    void Update()
    {
        Vector3 rot = LLever.localEulerAngles;
        float x = rot.x > 180f ? rot.x - 360f : rot.x;
        float z = rot.z > 180f ? rot.z - 360f : rot.z;

        if (Mathf.Abs(x) > Mathf.Abs(z))
        {
            // X축(앞뒤)이 더 많이 기울어졌을 때
            VirtualInput.D = x < -threshold;
            VirtualInput.A = x > threshold;

            // Z축 방향은 비활성화
            VirtualInput.Q = false;
            VirtualInput.E = false;
        }
        else
        {
            // Z축(좌우)이 더 많이 기울어졌을 때
            VirtualInput.Q = z < -threshold;
            VirtualInput.E = z > threshold;

            // X축 방향은 비활성화
            VirtualInput.D = false;
            VirtualInput.A = false;
        }
    }
}