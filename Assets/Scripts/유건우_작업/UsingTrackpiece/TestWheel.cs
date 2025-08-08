using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWheel : MonoBehaviour
{
    public Rigidbody driveCylinder;      // 원기둥의 Rigidbody
    public float torqueAmount = 500f;    // 토크 세기

    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            // driveCylinder의 로컬 Y축 방향으로 회전력 적용
            driveCylinder.AddTorque(driveCylinder.transform.up * torqueAmount, ForceMode.Force);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            // 역방향 회전
            driveCylinder.AddTorque(-driveCylinder.transform.up * torqueAmount, ForceMode.Force);
        }
    }
}
