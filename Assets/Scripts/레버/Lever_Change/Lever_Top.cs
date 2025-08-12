using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class Lever_Top : LeverBase
{
    protected override void LeverInput()
    {
        Vector3 rot = transform.localEulerAngles;
        if (rot.x > 180f) rot.x -= 360f;
        if (rot.z > 180f) rot.z -= 360f;

        var x = rot.x;
        var z = rot.z;

        if (Mathf.Abs(x) > Mathf.Abs(z))
        {
            // X축(앞뒤)이 더 많이 기울어졌을 때
            VirtualInput.inputs[(int)inputs[0]] = x < -threshold;   //S, A
            VirtualInput.inputs[(int)inputs[1]] = x > threshold;    //W, D

            // Z축 방향은 비활성화
            VirtualInput.inputs[(int)inputs[2]] = false;    //R, E
            VirtualInput.inputs[(int)inputs[3]] = false;    //F, Q
        }
        else
        {
            // X축 방향은 비활성화
            VirtualInput.inputs[(int)inputs[0]] = false;
            VirtualInput.inputs[(int)inputs[1]] = false;

            // Z축(좌우)이 더 많이 기울어졌을 때
            VirtualInput.inputs[(int)inputs[2]] = z > threshold;
            VirtualInput.inputs[(int)inputs[3]] = z < -threshold;
        }
    }
}
