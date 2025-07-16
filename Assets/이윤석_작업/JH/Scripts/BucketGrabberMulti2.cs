using System.Collections.Generic;
using UnityEngine;

public class BucketGrabberMulti2 : MonoBehaviour
{
    public enum Mode { Idle = 0, Dig = 1, Dump = 2 }

    [Header("Grab Zones (층별)")]
    public Collider[] grabZones;       // 각 층별 Grab Zone
    public int[]      zoneCapacities;  // 각 Zone 최대 용량

    Mode             _mode = Mode.Idle;
    Collider[]       _bucketCols;
    TerrainCollider  _terrainCol;
    List<Rigidbody>[] _grabbed;
    int              _currentZone = 0;
    bool             _grabbingEnabled = true;

    public Mode CurrentMode => _mode;

    void Awake()
    {
        // Grab Zone 초기화
        int n = grabZones.Length;
        _grabbed = new List<Rigidbody>[n];
        for (int i = 0; i < n; i++)
        {
            _grabbed[i] = new List<Rigidbody>();
            grabZones[i].enabled = (i == 0);
            var fwd = grabZones[i].gameObject.AddComponent<ZoneForwarder2>();
            fwd.Initialize(this, i);
        }

        // 충돌 무시 설정을 위해 버킷 콜라이더들 수집
        _bucketCols = GetComponentsInChildren<Collider>();
        _terrainCol = FindObjectOfType<TerrainDeformManager2>()
                          .GetComponent<TerrainCollider>();
    }

    /// <summary>
    /// 외부에서 모드를 변경할 때 호출하세요.
    /// </summary>
    public void SetMode(Mode newMode)
    {
        if (newMode == _mode) return;
        Mode old = _mode;
        _mode = newMode;
        OnModeChanged(old, _mode);
    }

    void OnModeChanged(Mode from, Mode to)
    {
        // Dig 모드 전환 시 2~n층 Zone 비우기
        if (to == Mode.Dig)  DetachZones(1);
        // Dump 모드 전환 시 전체 비우기
        if (to == Mode.Dump) DetachAllGrabbed();

        // Dig 모드일 때만 Terrain 충돌 무시
        bool ignoreTerrain = (to == Mode.Dig);
        foreach (var bc in _bucketCols)
            Physics.IgnoreCollision(bc, _terrainCol, ignoreTerrain);

        // Zone 활성화/비활성화
        for (int i = 0; i < grabZones.Length; i++)
            grabZones[i].enabled = false;

        switch (to)
        {
            case Mode.Idle:
                // 모든 Zone 활성화
                for (int i = 0; i < grabZones.Length; i++)
                    grabZones[i].enabled = true;
                _currentZone     = 0;
                _grabbingEnabled = true;
                break;
            case Mode.Dig:
                // 1층 Zone만 활성화
                grabZones[0].enabled = true;
                _currentZone     = 0;
                _grabbingEnabled = true;
                break;
            case Mode.Dump:
                // Grab 기능 비활성화
                _currentZone     = 0;
                _grabbingEnabled = false;
                break;
        }
    }

    public void Grab(int zoneIndex, GameObject soil)
    {
        if (!_grabbingEnabled || zoneIndex != _currentZone) return;

        var rb  = soil.GetComponent<Rigidbody>();
        var col = soil.GetComponent<Collider>();
        if (rb == null || col == null) return;

        // 입자 고정 및 부모로 이동
        rb.isKinematic = true;
        col.enabled     = false;
        soil.transform.SetParent(grabZones[zoneIndex].transform, true);
        soil.tag        = "SoilParticle";
        _grabbed[zoneIndex].Add(rb);

        // 제한 용량 초과 시 다음 Zone 활성화
        if (_grabbed[zoneIndex].Count >= zoneCapacities[zoneIndex])
        {
            grabZones[zoneIndex].enabled = false;
            if (zoneIndex + 1 < grabZones.Length)
            {
                _currentZone++;
                grabZones[_currentZone].enabled = true;
            }
            else _grabbingEnabled = false;
        }
    }

    void DetachZones(int startZone)
    {
        for (int z = startZone; z < _grabbed.Length; z++)
        {
            var list = _grabbed[z];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var rb = list[i];
                if (rb == null) { list.RemoveAt(i); continue; }
                rb.isKinematic = false;
                var col = rb.GetComponent<Collider>();
                col.enabled     = true;
                rb.gameObject.tag = "Untagged";
                rb.transform.SetParent(null, true);
                list.RemoveAt(i);
            }
        }
    }

    void DetachAllGrabbed() => DetachZones(0);

    class ZoneForwarder2 : MonoBehaviour
    {
        BucketGrabberMulti2 _owner;
        int                _zi;
        public void Initialize(BucketGrabberMulti2 o, int zi)
        {
            _owner = o; _zi = zi;
        }
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("SoilParticle"))
                _owner.Grab(_zi, other.gameObject);
        }
    }
}
