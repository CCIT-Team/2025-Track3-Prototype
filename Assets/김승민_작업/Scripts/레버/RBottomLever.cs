using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RBottomLever : MonoBehaviour
{
    public Transform RCrawl;
    public float threshold = 10f;

    void Update()
    {
        // 로컬 회전값 보정: 0~360 → -180~180
        float x = RCrawl.localEulerAngles.x;
        if (x > 180f) x -= 360f;

        VirtualInput.U = x < -threshold;

        VirtualInput.J = x > threshold;
    }
}