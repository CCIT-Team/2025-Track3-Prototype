using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LBottomLever : MonoBehaviour
{
    public Transform LCrawl;
    public float threshold = 10f;

    void Update()
    {
        // 로컬 회전값 보정: 0~360 → -180~180
        float x = LCrawl.localEulerAngles.x;

        if (x > 180f) x -= 360f;

        VirtualInput.T = x < -threshold;

        VirtualInput.G = x > threshold;
    }
}