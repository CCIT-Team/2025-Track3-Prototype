using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlopeGauge : MonoBehaviour
{
    public Transform excavator;
    public RectTransform pitchNeedle;
    public RectTransform rollNeedle;

    public float pitchMin = -30f, pitchMax = 30f;
    public float needleAngleMin = -90f, needleAngleMax = 90f;
    
    void Update()
    {
        Vector3 angles = excavator.eulerAngles;
        float pitch = angles.z > 180f ? angles.z - 360f : angles.z;
        float roll = angles.x > 180f ? angles.x - 360f : angles.x;

        float pitchT = Mathf.InverseLerp(pitchMin, pitchMax, pitch);
        float rollT = Mathf.InverseLerp(pitchMin, pitchMax, roll);

        float pitchAngle = Mathf.Lerp(needleAngleMin, needleAngleMax, pitchT);
        float rollAngle = Mathf.Lerp(needleAngleMin, needleAngleMax, rollT);

        pitchNeedle.localEulerAngles = new Vector3(0, 0, -pitchAngle);
        rollNeedle.localEulerAngles = new Vector3(0, 0, -rollAngle);
    }
}
