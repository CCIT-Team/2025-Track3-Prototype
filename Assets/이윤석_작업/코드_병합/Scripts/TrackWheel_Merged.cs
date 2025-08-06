using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TrackWheel_Merged : MonoBehaviour
{
    public WheelCollider[] leftWheels; // 왼쪽 트랙
    public WheelCollider[] rightWheels; // 오른쪽 트랙

    public float motorTorque = 500f;  // 가속 모터 속도
    public float brakeTorque = 1000f; // 브레이크 속도

    public Rigidbody body;           // 속도 측정을 위한 몸체


    void FixedUpdate()
    {
        // 현재 속도 계산
        Vector3 currentTurnSpeed = body.angularVelocity;

        Debug.Log(currentTurnSpeed);
        // 키 입력 처리
        float leftInput = Input.GetKey(KeyCode.T) ? 1 : Input.GetKey(KeyCode.G) ? -1 : 0;
        float rightInput = Input.GetKey(KeyCode.U) ? 1 : Input.GetKey(KeyCode.J) ? -1 : 0;

        // 왼쪽 바퀴 처리
        foreach (WheelCollider wc in leftWheels)
        {
            wc.motorTorque = leftInput * motorTorque;
            wc.brakeTorque = (leftInput == 0) ? brakeTorque : 0;
        }

        // 오른쪽 바퀴 처리
        foreach (WheelCollider wc in rightWheels)
        {
            wc.motorTorque = rightInput * motorTorque;
            wc.brakeTorque = (rightInput == 0) ? brakeTorque : 0;
        }
    }
}
