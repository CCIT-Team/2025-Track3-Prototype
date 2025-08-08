using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFollow : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;

    #region MonoBehaviour

    private void Start()
    {
        if (!targetTransform)
            Debug.LogWarning("타겟 트랜스폼이 비어있음!!! 너 큰일났어.");
    }

    private void Update()
    {
        FollowTarget(targetTransform);
    }

    #endregion


    private void FollowTarget(Transform target)
    {
        if (!target)
            return;

        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}