using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class ModelLinker : MonoBehaviour
{
    [SerializeField] GameObject targetObject;
    [SerializeField] GameObject modelObject;

                     Transform[] targetTransforms;
    [SerializeField] GameObject[] linkObjects;
    void Start()
    {
        targetTransforms = new Transform[linkObjects.Length];

        for (int i = 0; i < targetTransforms.Length; i++)
        {
            targetTransforms[i] = targetObject.transform.FindChildRecursive(linkObjects[i].name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        modelObject.transform.localRotation = targetObject.transform.localRotation;
        for (int i = 0; i < linkObjects.Length; i++)
        {
            linkObjects[i].transform.localRotation = targetTransforms[i].localRotation;
        }
    }
}
