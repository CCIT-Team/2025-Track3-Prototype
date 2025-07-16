using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages multiple grab zones on the bucket based on current mode (Idle, Dig, Dump).
/// Handles particle grabbing and release, and terrain collision toggling.
/// </summary>
public class BucketGrabberMulti : MonoBehaviour
{
    public enum Mode { Idle, Dig, Dump }

    [Header("Grab Zones")]
    [Tooltip("Colliders representing grab zones for each layer.")]
    [SerializeField] private Collider[] grabZones;
    [Tooltip("Max particle count per zone.")]
    [SerializeField] private int[] zoneCapacities;
    [Tooltip("Layer mask for soil particles to grab.")]
    [SerializeField] private LayerMask soilLayer;

    [Header("Collision Settings")]
    [Tooltip("Colliders on the bucket to ignore terrain collision.")]
    [SerializeField] private Collider[] bucketColliders;
    [Tooltip("TerrainCollider to toggle collision with.")]
    [SerializeField] private TerrainCollider terrainCollider;

    private Mode _mode = Mode.Idle;
    private List<Rigidbody>[] _grabbed;
    private int _currentZone = 0;
    private bool _grabbingEnabled = true;

    public Mode CurrentMode => _mode;

    void Awake()
    {
        int count = grabZones.Length;
        _grabbed = new List<Rigidbody>[count];
        for (int i = 0; i < count; i++)
        {
            _grabbed[i] = new List<Rigidbody>();
            // Only enable initial zone
            grabZones[i].enabled = (i == 0);
            // Attach forwarder
            var forwarder = grabZones[i].gameObject.AddComponent<ZoneForwarder>();
            forwarder.Initialize(this, i, soilLayer);
        }
        // Ensure bucket colliders set
        if (bucketColliders == null || bucketColliders.Length == 0)
            bucketColliders = GetComponentsInChildren<Collider>();
    }

    /// <summary>
    /// Switch grab mode and update zone activation & collision.
    /// </summary>
    public void SetMode(Mode newMode)
    {
        if (newMode == _mode) return;
        _mode = newMode;

        // 0) 모드 전환 직전, 이전에 잡아둔 입자 떼어내기
        if (newMode == Mode.Dig)
            DetachZones(startZone: 1);    // Dig 모드로 전환할 땐 2~n층을 비움
        else if (newMode == Mode.Dump)
            DetachAllZones();              // Dump 모드로 전환할 땐 전체 비움

        // 1) Terrain collision toggle
        bool ignoreTerrain = (_mode == Mode.Dig);
        foreach (var bc in bucketColliders)
            Physics.IgnoreCollision(bc, terrainCollider, ignoreTerrain);

        // 2) Grab Zone 활성화/비활성화
        for (int i = 0; i < grabZones.Length; i++)
            grabZones[i].enabled = false;

        switch (_mode)
        {
            case Mode.Idle:
                _grabbingEnabled = true;
                _currentZone     = 0;
                for (int i = 0; i < grabZones.Length; i++)
                    grabZones[i].enabled = true;
                break;

            case Mode.Dig:
                _grabbingEnabled = true;
                _currentZone     = 0;
                grabZones[0].enabled = true;
                break;

            case Mode.Dump:
                _grabbingEnabled = false;
                _currentZone     = 0;
                // 아무 것도 켜지 않음
                break;
        }
    }

    private void EnableAllZones()
    {
        for (int i = 0; i < grabZones.Length; i++)
            grabZones[i].enabled = true;
    }

    /// <summary>
    /// Attempt to grab a soil particle into the specified zone.
    /// </summary>
    public void Grab(int zoneIndex, GameObject soilObj)
    {
        if (!_grabbingEnabled || zoneIndex != _currentZone) return;

        // 태그로 판별
        if (!soilObj.CompareTag("SoilParticle")) return;

        // Rigidbody & Collider 체크...
        if (!soilObj.TryGetComponent<Rigidbody>(out var rb) ||
            !soilObj.TryGetComponent<Collider>(out var col))
            return;

        // 나머지 로직 동일
        rb.isKinematic = true;
        col.enabled = false;
        soilObj.transform.SetParent(grabZones[zoneIndex].transform, true);
        _grabbed[zoneIndex].Add(rb);

        // If capacity reached, move to next zone
        if (_grabbed[zoneIndex].Count >= zoneCapacities[zoneIndex])
        {
            grabZones[zoneIndex].enabled = false;
            if (zoneIndex + 1 < grabZones.Length)
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

    private void DetachZones(int startZone)
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
                if (col) col.enabled = true;
                rb.gameObject.tag = "Untagged";
                rb.transform.SetParent(null, worldPositionStays: true);
                list.RemoveAt(i);
            }
        }
    }

    private void DetachAllZones() => DetachZones(startZone: 0);

    /// <summary>
    /// Helper component forwarding trigger events to the main grabber.
    /// </summary>
    private class ZoneForwarder : MonoBehaviour
    {
        private BucketGrabberMulti _owner;
        private int _zoneIndex;
        private LayerMask _soilLayer;

        public void Initialize(BucketGrabberMulti owner, int zoneIndex, LayerMask soilLayer)
        {
            _owner = owner;
            _zoneIndex = zoneIndex;
            _soilLayer = soilLayer;
        }

        void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & _soilLayer) != 0)
                _owner.Grab(_zoneIndex, other.gameObject);
        }
    }
}
