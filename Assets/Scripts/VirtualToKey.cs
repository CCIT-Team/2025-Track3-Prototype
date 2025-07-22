using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualToKey : MonoBehaviour
{
    void Update()
    {
        Convert();
    }

    void Convert()
    {
        if (Input.GetKeyDown(KeyCode.W))
            VirtualInput.inputs[(int)EINPUT.W] = true;
        else if (Input.GetKeyUp(KeyCode.W))
            VirtualInput.inputs[(int)EINPUT.W] = false;

        if (Input.GetKeyDown(KeyCode.A))
            VirtualInput.inputs[(int)EINPUT.A] = true;
        else if(Input.GetKeyUp(KeyCode.A))
            VirtualInput.inputs[(int)EINPUT.A] = false;

        if (Input.GetKeyDown(KeyCode.S))
            VirtualInput.inputs[(int)EINPUT.S] = true;
        else if( Input.GetKeyUp(KeyCode.S))
            VirtualInput.inputs[(int)EINPUT.S] = false;

        if (Input.GetKeyDown(KeyCode.D))
            VirtualInput.inputs[(int)EINPUT.D] = true;
        else if (Input.GetKeyUp(KeyCode.D))
            VirtualInput.inputs[(int)EINPUT.D] = false;

        if (Input.GetKeyDown(KeyCode.Q))
            VirtualInput.inputs[(int)EINPUT.Q] = true;
        else if (Input.GetKeyUp(KeyCode.Q))
            VirtualInput.inputs[(int)EINPUT.Q] = false;

        if (Input.GetKeyDown(KeyCode.E))
            VirtualInput.inputs[(int)EINPUT.E] = true;
        else if (Input.GetKeyUp(KeyCode.E))
            VirtualInput.inputs[(int)EINPUT.E] = false;

        if (Input.GetKeyDown(KeyCode.R))
            VirtualInput.inputs[(int)EINPUT.R] = true;
        else if (Input.GetKeyUp(KeyCode.R))
            VirtualInput.inputs[(int)EINPUT.R] = false;

        if (Input.GetKeyDown(KeyCode.F))
            VirtualInput.inputs[(int)EINPUT.F] = true;
        else if (Input.GetKeyUp(KeyCode.F))
            VirtualInput.inputs[(int)EINPUT.F] = false;

        if (Input.GetKeyDown(KeyCode.T))
            VirtualInput.inputs[(int)EINPUT.T] = true;
        else if (Input.GetKeyUp(KeyCode.T))
            VirtualInput.inputs[(int)EINPUT.T] = false;

        if (Input.GetKeyDown(KeyCode.G))
            VirtualInput.inputs[(int)EINPUT.G] = true;
        else if (Input.GetKeyUp(KeyCode.G))
            VirtualInput.inputs[(int)EINPUT.G] = false;

        if (Input.GetKeyDown(KeyCode.U))
            VirtualInput.inputs[(int)EINPUT.U] = true;
        else if (Input.GetKeyUp(KeyCode.U))
            VirtualInput.inputs[(int)EINPUT.U] = false;

        if (Input.GetKeyDown(KeyCode.J))
            VirtualInput.inputs[(int)EINPUT.J] = true;
        else if (Input.GetKeyUp(KeyCode.J))
            VirtualInput.inputs[(int)EINPUT.J] = false;
    }
}
