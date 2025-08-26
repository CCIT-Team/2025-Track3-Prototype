using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inclinometer : MonoBehaviour
{
    [SerializeField] Transform targetTransform;
    [SerializeField] Camera frontCam;
    [SerializeField] Camera sideCam;

    [Header("UI")]
    [SerializeField] Text incinometerText;
    float roll = 0;
    float pitch = 0;

    Vector3 frontCamBasePos;
    Vector3 sideCamBasePos;

    private void Awake()
    {
        frontCamBasePos = frontCam.transform.localPosition;
        sideCamBasePos = sideCam.transform.localPosition;
    }

    private void Update()
    {
        CameraRot();
        UpdateUI();
    }

    void CameraRot()
    {
            frontCam.transform.localPosition = targetTransform.localToWorldMatrix * frontCamBasePos;
            frontCam.transform.LookAt(targetTransform.position + (Vector3)(targetTransform.localToWorldMatrix * new Vector3(frontCamBasePos.x, frontCamBasePos.y, 0)));
            sideCam.transform.localPosition = targetTransform.localToWorldMatrix * sideCamBasePos;
            sideCam.transform.LookAt(targetTransform.position + (Vector3)(targetTransform.localToWorldMatrix * new Vector3(0, sideCamBasePos.y, sideCamBasePos.z)));
    }

    void UpdateUI()
    {
        roll = 180>targetTransform.rotation.eulerAngles.z? targetTransform.rotation.eulerAngles.z : -(360 - targetTransform.rotation.eulerAngles.z);
        pitch = 180 > targetTransform.rotation.eulerAngles.x ? targetTransform.rotation.eulerAngles.x : -(360 - targetTransform.rotation.eulerAngles.x);

        incinometerText.text = "Roll \t: " + roll.ToString("F2") + "\n Pitch\t: " + pitch.ToString("F2");
    }
}
