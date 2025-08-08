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
        GameObjectPool pool = new GameObjectPool(_prefab, Vector3.zero, Quaternion.identity, null, _initialPoolCapacity, _maxCapacity);

        foreach (var i in _targets)
        {
            IPoolable poolable = null;
            if (i.TryGetComponent<IPoolable>(out poolable))
            {
                poolable.SetPoolInstance(pool);
            }

        }
    }
}
