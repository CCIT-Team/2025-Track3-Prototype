using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitialPoolInjector : MonoBehaviour
{
    [SerializeField]
    private GameObject _prefab;
    [SerializeField]
    private GameObject[] _targets;

    [SerializeField]
    private int _initialPoolCapacity;
    [SerializeField]
    private int _maxCapacity;
    // Start is called before the first frame update
    void Start()
    {
        GameObjectPool pool = new GameObjectPool(_prefab, Vector3.zero, Quaternion.identity, transform, _initialPoolCapacity, _maxCapacity);

        FindObjectOfType<BucketControllerMerged>().SetPoolInstance(pool);
    }
}
