using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class GameObjectPool
{
    private int _maxPoolSize;
    private GameObject _prefab = null;
    private Vector3 _initialSpawnPoint;
    private Quaternion _initialSpawnQuaternion;
    private Transform _initialSpawnParent = null;
    private Queue<GameObject> _gameObjectPool = null;

    public int PoolObjectCount => _gameObjectPool.Count;
    public int PoolMaxCount => _maxPoolSize;

    public void SetInitialSpawnPoint(Vector3 point)
    {
        _initialSpawnPoint = point;
    }

    public void SetInitialSpawnQuaternion(Quaternion quaternion)
    {
        _initialSpawnQuaternion = quaternion;
    }

    public void SetInitialSpawnParent(Transform parent)
    {
        _initialSpawnParent = parent;
    }

    private GameObject CreateNewObject()
    {
        GameObject spawnedObject = GameObject.Instantiate(_prefab, _initialSpawnPoint, _initialSpawnQuaternion);

        if (spawnedObject == null)
        {
            return null;
        }

        IPoolable poolableInterface = spawnedObject.GetComponent<IPoolable>();

        spawnedObject.SetActive(false);
        spawnedObject.transform.SetParent(_initialSpawnParent);

        if (poolableInterface == null)
        {
            Debug.Log("오브젝트가 IPoolable을 가지고 있지 않습니다");
            GameObject.Destroy(spawnedObject);
            return null;
        }

        poolableInterface.SetPoolInstance(this);
        return spawnedObject;
    }

    public GameObjectPool(GameObject prefab, Vector3 initialSpawnPoint, Quaternion initialSpawnQuaternion, Transform initialSpawnParent, int initialPoolCapacity = 32, int maxPoolSize = 64)
    {
        _gameObjectPool = new Queue<GameObject>(initialPoolCapacity);
        _maxPoolSize = maxPoolSize;
        _prefab = prefab;

        _initialSpawnPoint = initialSpawnPoint;
        _initialSpawnQuaternion = initialSpawnQuaternion;
        _initialSpawnParent = initialSpawnParent;

        for (int i = 0; i < initialPoolCapacity && i < maxPoolSize; i++)
        {
            GameObject spawnedObject = CreateNewObject();

            if (spawnedObject == null)
            {
                continue;
            }

            _gameObjectPool.Enqueue(spawnedObject);
        }
    }

    public void ReturnGameObject(GameObject returnedObject)
    {
        if (returnedObject == null)
        {
            return;
        }

        IPoolable poolableInterface = returnedObject.GetComponent<IPoolable>();

        if (poolableInterface == null || !poolableInterface.ComparePoolInstance(this))
        {
            return;
        }

        if (_gameObjectPool.Count >= _maxPoolSize)
        {
            GameObject.Destroy(returnedObject);
            return;
        }

        returnedObject.SetActive(false);
        returnedObject.transform.SetParent(_initialSpawnParent);
        returnedObject.transform.SetPositionAndRotation(_initialSpawnPoint, _initialSpawnQuaternion);
        _gameObjectPool.Enqueue(returnedObject);
    }

    public GameObject GetGameObject()
    {
        GameObject selectedGameObject = null;

        if (_gameObjectPool.Count <= 0)
        {
            selectedGameObject = CreateNewObject();
        }
        else
        {
            selectedGameObject = _gameObjectPool.Dequeue();
        }

        if (selectedGameObject != null)
        {
            selectedGameObject.SetActive(true);
        }

        return selectedGameObject;
    }

}
