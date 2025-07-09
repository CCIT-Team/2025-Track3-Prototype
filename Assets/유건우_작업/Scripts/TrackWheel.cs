using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackWheel : MonoBehaviour
{
    public WheelCollider[] leftWheels;
    public WheelCollider[] rightWheels;

    public float motorTorque = 500f;
    public float brakeTorque = 1000f;

    void FixedUpdate()
    {
        float leftInput = Input.GetKey(KeyCode.T) ? 1 : Input.GetKey(KeyCode.G) ? -1 : 0;
        float rightInput = Input.GetKey(KeyCode.U) ? 1 : Input.GetKey(KeyCode.J) ? -1 : 0;

        foreach (WheelCollider wc in leftWheels)
        {
            wc.motorTorque = leftInput * motorTorque;
            wc.brakeTorque = (leftInput == 0) ? brakeTorque : 0;
        }

        foreach (WheelCollider wc in rightWheels)
        {
            wc.motorTorque = rightInput * motorTorque;
            wc.brakeTorque = (rightInput == 0) ? brakeTorque : 0;
        }
    }
}
