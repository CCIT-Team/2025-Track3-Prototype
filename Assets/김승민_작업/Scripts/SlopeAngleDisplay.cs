using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SlopeAngleDisplay : MonoBehaviour
{
    public Transform excavatorBody; // 굴삭기 본체
    public TextMeshProUGUI pitchText; // 앞뒤 경사
    public TextMeshProUGUI rollText;  // 좌우 경사

    void Update()
    {
        Vector3 angles = excavatorBody.eulerAngles;

        // Unity의 회전은 0~360도 → -180~180도로 보정
        float pitch = angles.z > 180f ? angles.z - 360f : angles.z;
        float roll = angles.x > 180f ? angles.x - 360f : angles.x;

        // 표시
        pitchText.text = $"Pitch(Front): {pitch:F1}°";
        rollText.text = $"Roll(Side): {roll:F1}°";
    }
}
