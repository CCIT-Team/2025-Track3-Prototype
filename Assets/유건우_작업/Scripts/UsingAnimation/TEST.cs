using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TEST : MonoBehaviour
{
    WheelCollider[] leftWheels;
    WheelCollider[] rightWheels;

    void Awake()
    {
        GameObject leftTrack = GameObject.Find("LeftTrack");
        GameObject rightTrack = GameObject.Find("RightTrack");

        if (leftTrack != null)
            leftWheels = leftTrack.GetComponentsInChildren<WheelCollider>();

        if (rightTrack != null)
            rightWheels = rightTrack.GetComponentsInChildren<WheelCollider>();
    }

    void Update()
    {
        float leftAvgRpm = 0f, rightAvgRpm = 0f;
        float leftAvgTorque = 0f, rightAvgTorque = 0f;

        if (leftWheels != null && leftWheels.Length > 0)
        {
            foreach (WheelCollider wc in leftWheels)
            {
                leftAvgRpm += wc.rpm;
                leftAvgTorque += wc.motorTorque;
            }
            leftAvgRpm /= leftWheels.Length;
            leftAvgTorque /= leftWheels.Length;
        }

        if (rightWheels != null && rightWheels.Length > 0)
        {
            foreach (WheelCollider wc in rightWheels)
            {
                rightAvgRpm += wc.rpm;
                rightAvgTorque += wc.motorTorque;
            }
            rightAvgRpm /= rightWheels.Length;
            rightAvgTorque /= rightWheels.Length;
        }

        Debug.Log($"[RPM] LEFT: {leftAvgRpm:F1}, RIGHT: {rightAvgRpm:F1} | [Torque] LEFT: {leftAvgTorque:F1}, RIGHT: {rightAvgTorque:F1}");
    }
}
