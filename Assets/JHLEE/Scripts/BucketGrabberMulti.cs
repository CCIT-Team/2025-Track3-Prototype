using System.Collections.Generic;
using UnityEngine;

public class BucketGrabberMulti : MonoBehaviour
{
    public enum Mode { Idle = 0, Dig = 1, Dump = 2 }

    [Header("Grab Zones (층별)")]
    public Collider[] grabZones;    // 0=1층, 1=2층, 2=3층
    public int[]      zoneCapacities;

    [Header("모드 전환 키")]
    public KeyCode nextModeKey = KeyCode.E;   // Idle→Dig→Dump
    public KeyCode prevModeKey = KeyCode.Q;   // Dump→Dig→Idle

    [Header("버킷 회전")]
    public Transform bucketTransform;
    public Vector3   idleEuler = Vector3.zero;
    public Vector3   digEuler  = new Vector3(-45,0,0);
    public Vector3   dumpEuler = new Vector3(-90,0,0);
    public float     lerpSpeed = 5f;

    Mode           _mode = Mode.Idle;
    Quaternion     _targetRot;
    Collider[]     _bucketCols;
    TerrainCollider _terrainCol;
    BucketController _bucketCtrl;
    List<Rigidbody>[] _grabbed;
    int            _currentZone = 0;
    bool           _grabbingEnabled = true;

    public Mode CurrentMode => _mode;

    void Awake()
    {
        int n = grabZones.Length;
        _grabbed = new List<Rigidbody>[n];
        for (int i = 0; i < n; i++)
        {
            _grabbed[i] = new List<Rigidbody>();
            grabZones[i].enabled = (i == 0);
            var fwd = grabZones[i].gameObject.AddComponent<ZoneForwarder>();
            fwd.Initialize(this, i);
        }

        _bucketCols  = GetComponentsInChildren<Collider>();
        _terrainCol  = FindObjectOfType<TerrainDeformManager>()
                          .GetComponent<TerrainCollider>();
        _bucketCtrl  = GetComponent<BucketController>();

        _targetRot = Quaternion.Euler(idleEuler);
        if (bucketTransform != null)
            bucketTransform.localRotation = _targetRot;
    }

    void Update()
    {
        if (Input.GetKeyDown(nextModeKey) && _mode != Mode.Dump)
            SetMode(_mode + 1);
        if (Input.GetKeyDown(prevModeKey) && _mode != Mode.Idle)
            SetMode(_mode - 1);

        switch (_mode)
        {
          case Mode.Dig:  _targetRot = Quaternion.Euler(digEuler);  break;
          case Mode.Dump: _targetRot = Quaternion.Euler(dumpEuler); break;
          default:        _targetRot = Quaternion.Euler(idleEuler); break;
        }
        bucketTransform.localRotation = Quaternion.Slerp(
            bucketTransform.localRotation,
            _targetRot,
            Time.deltaTime * lerpSpeed);
    }

    void SetMode(Mode newMode)
    {
        if (newMode == _mode) return;
        Mode old = _mode;
        _mode = newMode;
        OnModeChanged(old, _mode);
    }

    void OnModeChanged(Mode from, Mode to)
    {
        // Dig 모드 전환 시 2,3층에서 잡힌 입자 방출
        if (to == Mode.Dig)
            DetachZones(1);
        // Dump 모드 전환 시 전체 방출
        if (to == Mode.Dump)
            DetachAllGrabbed();

        bool ignoreTerrain = (to == Mode.Dig);
        foreach (var bc in _bucketCols)
            Physics.IgnoreCollision(bc, _terrainCol, ignoreTerrain);

        // 모든 GrabZone 콜라이더 비활성화
        for (int i = 0; i < grabZones.Length; i++)
            grabZones[i].enabled = false;

        // 모드별로 필요한 존만 활성화
        switch (to)
        {
            case Mode.Idle:
                for (int i = 0; i < grabZones.Length; i++)
                    grabZones[i].enabled = true;
                _currentZone     = 0;
                _grabbingEnabled = true;
                break;

            case Mode.Dig:
                grabZones[0].enabled = true;
                _currentZone     = 0;
                _grabbingEnabled = true;
                break;

            case Mode.Dump:
                _currentZone     = 0;
                _grabbingEnabled = false;
                break;
        }
    }

    public void Grab(int zoneIndex, GameObject soil)
    {
        if (!_grabbingEnabled || zoneIndex != _currentZone) return;

        var rb = soil.GetComponent<Rigidbody>();
        var col = soil.GetComponent<Collider>();
        if (rb == null || col == null) return;

        rb.isKinematic = true;
        col.enabled    = false;
        soil.transform.SetParent(grabZones[zoneIndex].transform, true);
        // soilParticle 태그 유지만 하면 새로 생성된 입자로만 Grab
        // 기존 토출된 입자는 태그 제거해서 재 Grab 방지
        soil.tag = "SoilParticle";
        _grabbed[zoneIndex].Add(rb);

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
                col.enabled = true;
                // Detach 후 SoilParticle 태그 제거
                rb.gameObject.tag = "Untagged";
                rb.transform.SetParent(null, true);
                list.RemoveAt(i);
            }
        }
    }

    void DetachAllGrabbed() => DetachZones(0);

    class ZoneForwarder : MonoBehaviour
    {
        BucketGrabberMulti _owner;
        int                _zi;
        public void Initialize(BucketGrabberMulti o, int zi)
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
