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
            grabZones[i].enabled = (i == 0);
            var forwarder = grabZones[i].gameObject.AddComponent<ZoneForwarderMerged>();
            forwarder.Initialize(this, i, soilLayer);
        }
    }

    public void SetMode(Mode newMode)
    {
        if (newMode == _mode) return;
        _mode = newMode;

        // Soft detach on Dig (zones >0) and Dump (all zones)
        if (_mode == Mode.Dig)
            SoftDetachZones(1);
        else if (_mode == Mode.Dump)
            SoftDetachZones(0);

        bool ignoreTerrain = (_mode == Mode.Dig);
        foreach (var bc in bucketColliders)
            Physics.IgnoreCollision(bc, terrainCollider, ignoreTerrain);

        for (int i = 0; i < grabZones.Length; i++)
            grabZones[i].enabled = false;

        _grabbingEnabled = (_mode != Mode.Dump);
        _currentZone = 0;
        if (_mode == Mode.Idle)
            for (int i = 0; i < grabZones.Length; i++)
                grabZones[i].enabled = true;
        else if (_mode == Mode.Dig)
            grabZones[0].enabled = true;
    }

    public void Grab(int zoneIndex, GameObject soilObj)
    {
        if (!_grabbingEnabled || zoneIndex != _currentZone) return;
        if (!soilObj.CompareTag("SoilParticle")) return;
        if (!soilObj.TryGetComponent<Rigidbody>(out var rb) || !soilObj.TryGetComponent<Collider>(out var col)) return;

        rb.isKinematic = true;
        col.enabled = false;
        soilObj.transform.SetParent(grabZones[zoneIndex].transform, true);
        _grabbed[zoneIndex].Add(rb);

        if (_grabbed[zoneIndex].Count >= zoneCapacities[zoneIndex])
        {
            grabZones[zoneIndex].enabled = false;
            if (zoneIndex + 1 < grabZones.Length)
            {
                _currentZone = zoneIndex + 1;
                grabZones[_currentZone].enabled = true;
            }
            else
                _grabbingEnabled = false;
        }
    }

    /// <summary>
    /// Allow particles to drop naturally under gravity without bouncing.
    /// </summary>
    private void SoftDetachZones(int startZone)
    {
        for (int z = startZone; z < _grabbed.Length; z++)
        {
            var list = _grabbed[z];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var rb = list[i];
                if (rb == null) { list.RemoveAt(i); continue; }

                // Restore physics
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Ensure collider has zero bounce
                var col = rb.GetComponent<Collider>();
                if (col && col.material != null)
                {
                    col.material.bounciness = 0f;
                    col.material.bounceCombine = PhysicMaterialCombine.Minimum;
                }

                // Detach from bucket hierarchy
                rb.transform.SetParent(null, true);
                list.RemoveAt(i);
            }
        }
    }

    private class ZoneForwarderMerged : MonoBehaviour
    {
        private BucketGrabberMultiMerged _owner;
        private int _zoneIndex;
        private LayerMask _soilLayer;
        private Collider _zoneCollider;

        public void Initialize(BucketGrabberMultiMerged owner, int zoneIndex, LayerMask soilLayer)
        {
            _owner = owner;
            _zoneIndex = zoneIndex;
            _soilLayer = soilLayer;
            _zoneCollider = GetComponent<Collider>();
        }

        void OnTriggerStay(Collider other)
        {
            if (((1 << other.gameObject.layer) & _soilLayer) != 0)
            {
                if (_zoneCollider.bounds.Contains(other.transform.position))
                    _owner.Grab(_zoneIndex, other.gameObject);
            }
        }
    }
}
