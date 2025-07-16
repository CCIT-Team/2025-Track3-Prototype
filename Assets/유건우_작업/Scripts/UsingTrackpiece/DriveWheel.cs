using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveWheel : MonoBehaviour
{
    public float motorForce = 200f;            // 얼마나 강하게 밀지
    public float motorSpeed = 300f;            // 얼마나 빠르게 돌지 (회전속도)
    public bool reverse = false;               // 방향 전환 (뒤로 가기 등)

    private HingeJoint hinge;

    void Start()
    {
        hinge = GetComponent<HingeJoint>();

        if (hinge == null)
        {
            Debug.LogError("HingeJoint가 이 오브젝트에 없습니다!");
            return;
        }

        JointMotor motor = hinge.motor;
        motor.force = motorForce;
        motor.targetVelocity = reverse ? -motorSpeed : motorSpeed;
        motor.freeSpin = false;

        hinge.motor = motor;
        hinge.useMotor = true;
    }
}

