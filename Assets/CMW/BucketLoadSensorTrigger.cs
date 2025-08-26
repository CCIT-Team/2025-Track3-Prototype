using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BucketLoadSensorTrigger : MonoBehaviour
{
    [Header("Filter")]
    [Tooltip("센서에 반응할 레이어(돌/토사/가지 등). 비워두면 전부 허용.")]
    public LayerMask filterLayers = ~0;
    [Tooltip("이 질량(kg)보다 작은 Rigidbody는 무시")]
    public float minRbMass = 0.05f;
    [Tooltip("키네마틱 Rigidbody도 집계할지 여부")]
    public bool includeKinematic = true;

    [Header("Debug (읽기 전용)")]
    public float totalMass;   // ← 컨트롤러가 읽는 필드
    public int countInside; // 센서 안 고유 Rigidbody 개수

    // 내부
    readonly HashSet<Rigidbody> _inside = new HashSet<Rigidbody>();
    Collider _col;

    void Reset()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void OnValidate()
    {
        _col = GetComponent<Collider>();
        if (_col && !_col.isTrigger) _col.isTrigger = true;
        minRbMass = Mathf.Max(0f, minRbMass);
    }

    void OnDisable()
    {
        _inside.Clear();
        totalMass = 0f;
        countInside = 0;
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (!IsValidRb(rb)) return;

        if (_inside.Add(rb))
        {
            totalMass += rb.mass;
            countInside = _inside.Count;
        }
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;

        if (_inside.Remove(rb))
        {
            totalMass = Mathf.Max(0f, totalMass - rb.mass);
            countInside = _inside.Count;
        }
    }

    bool IsValidRb(Rigidbody rb)
    {
        if (rb == null) return false;
        if (((1 << rb.gameObject.layer) & filterLayers) == 0) return false;
        if (!includeKinematic && rb.isKinematic) return false;
        if (rb.mass < minRbMass) return false;
        // 자기 자신(버킷) 계열 무시
        if (rb.transform.IsChildOf(transform.root)) return false;
        return true;
    }

    // 보기 좋게
    void OnDrawGizmosSelected()
    {
        if (!TryGetComponent<Collider>(out var c)) return;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        var m = c.transform.localToWorldMatrix;
        Gizmos.matrix = m;
        if (c is BoxCollider b)
        {
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
