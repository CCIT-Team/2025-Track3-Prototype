using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever_Bottom : LeverBase
{
    protected override void LeverInput()
    {
        // 로컬 회전값 보정: 0~360 → -180~180
        var x = transform.localEulerAngles.x;

        if (x > 180f) x -= 360f;

        VirtualInput.inputs[(int)inputs[0]] = x < -threshold;   //J, G 

        VirtualInput.inputs[(int)inputs[1]] = x > threshold;    //U, T
    }
}
