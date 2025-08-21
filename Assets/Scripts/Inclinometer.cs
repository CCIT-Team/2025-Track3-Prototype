using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Inclinometer : MonoBehaviour
{
    [SerializeField] Transform targetTransform;
    [SerializeField] Camera frontCam;
    [SerializeField] Camera sideCam;

    Vector3 frontCamBasePos;
    Vector3 sideCamBasePos;

    private void Awake()
    {
        frontCamBasePos = frontCam.transform.localPosition;
        sideCamBasePos = sideCam.transform.localPosition;
    }

    private void Update()
    {
        frontCam.transform.localPosition = targetTransform.localToWorldMatrix * frontCamBasePos;
        frontCam.transform.LookAt(targetTransform.position + (Vector3)(targetTransform.localToWorldMatrix * new Vector3(frontCamBasePos.x, frontCamBasePos.y, 0)));
        sideCam.transform.localPosition = targetTransform.localToWorldMatrix * sideCamBasePos;
        sideCam.transform.LookAt(targetTransform.position + (Vector3)(targetTransform.localToWorldMatrix * new Vector3(0, sideCamBasePos.y, sideCamBasePos.z)));
    }
}
