using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BucketPushSensorTrigger : MonoBehaviour
{
    [Tooltip("굴착기 루트(이 트랜스폼 자식은 자기 자신으로 판단하여 무시)")]
    public Transform ignoreSelfRoot;
    [Tooltip("IsKinematic 물체도 저항으로 셀지")]
    public bool includeKinematic = true;

    [Tooltip("센서의 +Z(forward) 방향으로 밀 때 유효 질량 가중치 적용")]
    public bool useDirectionalWeight = true;

    [Tooltip("현재 센서에 닿은 모든 RB 질량 합(kg)")]
    public float totalMass;
    [Tooltip("forward 방향으로 가중한 유효 질량(kg) — 컨트롤러가 이 값을 주로 사용")]
    public float effectiveMass;
    [Tooltip("Kinematic이나 굉장히 무거운 물체가 포함되어 있는지")]
    public bool hasKinematic;

    readonly HashSet<Rigidbody> _in = new();

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 반드시 Trigger
    }

    void OnTriggerEnter(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;
        if (!includeKinematic && rb.isKinematic) return;
        if (ignoreSelfRoot && rb.transform.IsChildOf(ignoreSelfRoot)) return;
        _in.Add(rb);
        Recalc();
    }

    void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;
        if (_in.Remove(rb)) Recalc();
    }

    void OnDisable()
    {
        _in.Clear();
        totalMass = effectiveMass = 0f;
        hasKinematic = false;
    }

    void Recalc()
    {
        float sum = 0f, eff = 0f;
        bool kin = false;
        Vector3 fwd = transform.forward;

        foreach (var rb in _in)
        {
            if (!rb) continue;
            float m = Mathf.Max(0f, rb.mass);
            sum += m;

            if (useDirectionalWeight)
            {
                // 센서 → 대상 방향이 forward(+Z)에 얼마나 정렬됐는지로 가중
                Vector3 dir = (rb.worldCenterOfMass - transform.position).normalized;
                float w = Mathf.Max(0f, Vector3.Dot(fwd, dir)); // 뒤에 있으면 0
                eff += m * Mathf.Lerp(0.5f, 1f, w);             // 정면일수록 가중↑
            }
            else eff += m;

            kin |= rb.isKinematic;
        }

        totalMass = sum;
        effectiveMass = eff;
        hasKinematic = kin;
    }
}
