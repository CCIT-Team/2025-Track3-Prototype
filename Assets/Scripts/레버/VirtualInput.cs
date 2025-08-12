using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EINPUT
{
    None = 0,
    Q,
    W,
    D,
    A,
    E,
    S,
    R,
    F,
    T,
    G,
    U,
    J,
    Count
}

public static class VirtualInput
{
    public static bool[] inputs = Enumerable.Repeat(false, (int)EINPUT.Count).ToArray();
    public static bool Q = false;
    public static bool W = false;
    public static bool D = false;
    public static bool A = false;
    public static bool E = false;
    public static bool S = false;
    public static bool R = false;
    public static bool F = false;
    public static bool T = false;
    public static bool G = false;
    public static bool U = false;
    public static bool J = false;
}