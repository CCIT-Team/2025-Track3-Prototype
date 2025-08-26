using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EINPUT
{
    None = 0,
    SwingLeft,
    SwingRight,
    BoomUp,
    BoomDown,
    ArmUp,
    ArmDown,
    BucketUp,
    BucketDown,
    WheelRF,
    WheelRB,
    WheelLF,
    WheelLB,
    Count
}

public static class VirtualInput
{
    public static bool[] inputs = Enumerable.Repeat(false, (int)EINPUT.Count).ToArray();
}