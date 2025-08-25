using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TargetMarker : MonoBehaviour
{
    public GameObject cam;
    public TextMeshProUGUI distanceText;
    public Transform target;


    void Update()
    {
        transform.LookAt(cam.transform.position);

        float distance = Vector3.Distance(cam.transform.position, target.position);

        if (distanceText != null)
        {
            distanceText.text = $"{distance:F1} m";

            distanceText.transform.LookAt(cam.transform);
            distanceText.transform.Rotate(0, 180f, 0);
        }
    }

    void LateUpdate()
    {
        float distance = Vector3.Distance(cam.transform.position, transform.position);
        float scaleFactor = distance * 0.05f;
        float basefontSize = distance * 0.05f;

        //transform.localScale = Vector3.one * scaleFactor;
        distanceText.fontSize = basefontSize;
    }
}
