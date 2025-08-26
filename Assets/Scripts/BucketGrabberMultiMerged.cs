using System.Collections.Generic;
using UnityEngine;

public class BucketGrabberMultiMerged : MonoBehaviour
{
    public Mode CurrentMode => _mode;
    public enum Mode { Idle, Dig, Dump }

    [Header("Grab Zones")]
    [SerializeField] private Collider[] grabZones;
    [SerializeField] private int[] zoneCapacities;
    [SerializeField] private LayerMask soilLayer;

    [Header("Collision Settings")]
    [SerializeField] private Collider[] bucketColliders;
    [SerializeField] private TerrainCollider terrainCollider;

    private Mode _mode = Mode.Idle;
    private List<Rigidbody>[] _grabbed;
    private int _currentZone = 0;
    private bool _grabbingEnabled = true;

    void Awake()
    {
        int count = grabZones.Length;
        _grabbed = new List<Rigidbody>[count];
        for (int i = 0; i < count; i++)
        {
            _grabbed[i] = new List<Rigidbody>();
            if (grabZones[i] != null)
            {
                grabZones[i].enabled = (i == 0);
                var forwarder = grabZones[i].GetComponent<ZoneForwarderMerged>();
                if (forwarder != null) forwarder.Initialize(this, i, soilLayer);
            }
            else
            {
                Debug.LogError($"[BucketGrabber] grabZones[{i}]가 비었습니다.", this);
            }
        }
    }

    void LateUpdate()
    {
        // 실패 복구: Dump가 아닌데 남아 있는 Grabbed를 정리
        if (_mode != Mode.Dump && TotalGrabbedCount() > 0)
        {
            if (_mode == Mode.Dig) SoftDetachZonesPublic(1);
            else ForceReleaseAll();
        }
    }

    public void SetMode(Mode newMode)
    {
        if (newMode == _mode) return;
        _mode = newMode;

        // Dig: zone1+ 해제, Dump: 전 존 해제
        if (_mode == Mode.Dig) SoftDetachZones(1);
        else if (_mode == Mode.Dump) ForceReleaseAll();

        bool ignoreTerrain = (_mode == Mode.Dig);
        if (terrainCollider != null && bucketColliders != null)
        {
            foreach (var bc in bucketColliders)
                if (bc != null) Physics.IgnoreCollision(bc, terrainCollider, ignoreTerrain);
        }

        for (int i = 0; i < grabZones.Length; i++)
            if (grabZones[i] != null) grabZones[i].enabled = false;

        _grabbingEnabled = (_mode != Mode.Dump);
        _currentZone = 0;

        if (_mode == Mode.Idle)
        {
            for (int i = 0; i < grabZones.Length; i++)
                if (grabZones[i] != null) grabZones[i].enabled = true;
        }
        else if (_mode == Mode.Dig)
        {
            if (grabZones.Length > 0 && grabZones[0] != null)
                grabZones[0].enabled = true;
        }
    }

    public void Grab(int zoneIndex, GameObject soilObj)
    {
        if (!_grabbingEnabled || zoneIndex != _currentZone) return;
        if (soilObj == null) return;
        if (!soilObj.CompareTag("SoilParticle")) return;

        if (!soilObj.TryGetComponent<Rigidbody>(out var rb) ||
            !soilObj.TryGetComponent<Collider>(out var col)) return;

        // Grab 표시
        soilObj.tag = "GrabbedParticle";
        rb.isKinematic = true;
        rb.detectCollisions = false;
        if (col) col.enabled = false;

        if (zoneIndex >= 0 && zoneIndex < grabZones.Length && grabZones[zoneIndex] != null)
            soilObj.transform.SetParent(grabZones[zoneIndex].transform, true);

        _grabbed[zoneIndex].Add(rb);

        // 용량 관리
        if (zoneCapacities != null && zoneIndex < zoneCapacities.Length &&
            _grabbed[zoneIndex].Count >= zoneCapacities[zoneIndex])
        {
            if (grabZones[zoneIndex] != null) grabZones[zoneIndex].enabled = false;
            if (zoneIndex + 1 < grabZones.Length && grabZones[zoneIndex + 1] != null)
            {
                _currentZone = zoneIndex + 1;
                grabZones[_currentZone].enabled = true;
            }
            else
            {
                _grabbingEnabled = false;
            }
        }
    }

    /// <summary>전 존 강제 해제(Dump/복구 용)</summary>
    public void ForceReleaseAll()
    {
        SoftDetachZones(0);
    }

    /// <summary>외부에서 zone1+만 해제하고 싶을 때 호출</summary>
    public void SoftDetachZonesPublic(int startZone) => SoftDetachZones(startZone);

    /// <summary>총 잡힌 수</summary>
    public int TotalGrabbedCount()
    {
        int sum = 0;
        if (_grabbed != null)
            for (int z = 0; z < _grabbed.Length; z++) sum += _grabbed[z].Count;
        return sum;
    }

    /// <summary>
    /// startZone: 0이면 전 존 해제, 1이면 zone0 유지
    /// </summary>
    private void SoftDetachZones(int startZone)
    {
        if (_grabbed == null) return;

        for (int z = Mathf.Clamp(startZone, 0, _grabbed.Length - 1); z < _grabbed.Length; z++)
        {
            var list = _grabbed[z];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var rb = list[i];
                if (rb == null) { list.RemoveAt(i); continue; }

                var go = rb.gameObject;

                // 부모 해제
                rb.transform.SetParent(null, true);

                // 태그 복원
                go.tag = "SoilParticle";

                // 콜라이더/충돌 복원
                if (rb.TryGetComponent<Collider>(out var col) && col != null)
                    col.enabled = true;

                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.constraints = RigidbodyConstraints.None;

                // Terrain에 묻히지 않게 살짝 띄우기
                var terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    Vector3 p = rb.position;
                    float gy = terrain.SampleHeight(p) + terrain.transform.position.y;
                    if (p.y < gy + 0.01f) p.y = gy + 0.01f;
                    rb.position = p;
                }

                // 아주 약한 초기속도(정지관성 해소)
                if (rb.velocity.sqrMagnitude < 1e-3f) rb.velocity = Vector3.down * 0.1f;

                list.RemoveAt(i);
            }
        }
    }
}
