using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class KSM_TrackWheel : MonoBehaviour
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
        float leftInput = VirtualInput.inputs[(int)EINPUT.T] ? 1 : VirtualInput.inputs[(int)EINPUT.G] ? -1 : 0;
        float rightInput = VirtualInput.inputs[(int)EINPUT.U] ? 1 : VirtualInput.inputs[(int)EINPUT.J] ? -1 : 0;

        Debug.Log($"T : {VirtualInput.inputs[(int)EINPUT.T]}, G : {VirtualInput.inputs[(int)EINPUT.G]}");

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
