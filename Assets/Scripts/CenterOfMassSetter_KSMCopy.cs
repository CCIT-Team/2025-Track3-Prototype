using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterOfMassSetter_KSMCopy: MonoBehaviour
{
    [SerializeField] public Vector3 centerOfMassOffset; // 아래로 이동

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass = centerOfMassOffset;
        }
    }
}
