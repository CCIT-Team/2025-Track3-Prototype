using UnityEngine;

public class RLeverControl : MonoBehaviour
{
    public Transform RLever;

    public float threshold = 10f;   

    void Update()
    {
        Vector3 rot = RLever.localEulerAngles;
        float x = rot.x > 180f ? rot.x - 360f : rot.x;
        float z = rot.z > 180f ? rot.z - 360f : rot.z;

        if (Mathf.Abs(x) > Mathf.Abs(z))
        {
            // X축(앞뒤)이 더 많이 기울어졌을 때
            VirtualInput.W = x < -threshold;
            VirtualInput.S = x > threshold;

            // Z축 방향은 비활성화
            VirtualInput.R = false;
            VirtualInput.F = false;
        }
        else
        {
            // Z축(좌우)이 더 많이 기울어졌을 때
            VirtualInput.R = z < -threshold;
            VirtualInput.F = z > threshold;

            // X축 방향은 비활성화
            VirtualInput.W = false;
            VirtualInput.S = false;
        }
    }
}
