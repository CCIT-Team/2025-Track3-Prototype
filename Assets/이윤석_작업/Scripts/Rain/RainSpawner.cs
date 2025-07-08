using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainSpawner : MonoBehaviour
{
    [SerializeField]
    private float _maxInterval = 0.1f;
    [SerializeField]
    private GameObject _prefab;


    [SerializeField]
    private Vector2 _spawnAreaSize;

    private GameObjectPool _pool;

    [SerializeField]
    private int init = 20, max = 60, cnt;

    void Start()
    {
        _pool = new GameObjectPool(_prefab, transform.position, Quaternion.identity, this.gameObject.transform, init, max);
    }

    void Rotate(ref float x, ref float y, float rad)
    {
        float rotX = Mathf.Cos(rad) * x - Mathf.Sin(rad) * y;
        float rotY = Mathf.Sin(rad) * x + Mathf.Cos(rad) * y;
        x = rotX;
        y = rotY;
    }

    void CalcPos(ref Vector3 pos)
    {
        float rad = Mathf.Deg2Rad * transform.eulerAngles.y * -1;
        pos.x = UnityEngine.Random.Range(transform.position.x, transform.position.x + _spawnAreaSize.x);
        pos.z = UnityEngine.Random.Range(transform.position.z, transform.position.z + _spawnAreaSize.y);
        pos.x -= transform.position.x;
        pos.z -= transform.position.z;

        Rotate(ref pos.x, ref pos.z, rad);

        pos.x += transform.position.x;
        pos.z += transform.position.z;
    }

    IEnumerator spawn()
    {
        Vector3 pos = transform.position;

        _pool.SetInitialSpawnPoint(transform.position);

        for (int i = 0; i < cnt; i++)
        {
            GameObject item = _pool.GetGameObject();
            CalcPos(ref pos);
            item.transform.position = pos;
            yield return new WaitForSeconds(UnityEngine.Random.Range(0, _maxInterval));
        }
    }

    [ContextMenu("spawn")]
    public void Func()
    {
        StartCoroutine(spawn());
    }
}
