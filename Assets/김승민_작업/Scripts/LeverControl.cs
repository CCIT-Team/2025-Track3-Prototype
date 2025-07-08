using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeverControl : MonoBehaviour
{
    public Transform controlledArm; // 연결된 굴삭기 Arm
    public float minAngle = -30f;
    public float maxAngle = 30f;
    public float armMinRotation = 0f;
    public float armMaxRotation = 60f;

    void Update()
    {
        // 로컬 X 회전값 가져오기
        float leverAngle = transform.localEulerAngles.x;
        if (leverAngle > 180f) leverAngle -= 360f;

        // 레버 각도를 [minAngle, maxAngle] → [0,1]로 정규화
        float t = Mathf.InverseLerp(minAngle, maxAngle, leverAngle);
        float armAngle = Mathf.Lerp(armMinRotation, armMaxRotation, t);

        controlledArm.localRotation = Quaternion.Euler(armAngle, 0f, 0f);
    }
}
