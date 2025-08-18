using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneForwarderMerged : MonoBehaviour
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