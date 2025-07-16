using System.Collections;
using UnityEngine;

public class Raindrop : MonoBehaviour, IPoolable
{
    [SerializeField]
    private float _waitTime = 10;
    private GameObjectPool _pool;
    private Coroutine _coroutine;
    private Rigidbody _rb;
    [SerializeField]
    private Vector3 _initialVelocity;
    
    public bool ComparePoolInstance(GameObjectPool poolInstance)
    {
        return _pool == poolInstance;
    }

    public void SetPoolInstance(GameObjectPool poolInstance)
    {
        _pool = poolInstance;
    }

    private IEnumerator ReturnSelf()
    {
        yield return new WaitForSeconds(_waitTime);
        _pool.ReturnGameObject(this.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("물 생성");
        StopCoroutine(_coroutine);
        _pool.ReturnGameObject(this.gameObject);
    }
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }
    void OnEnable()
    {
        _coroutine = StartCoroutine(ReturnSelf());
        _rb.velocity = _initialVelocity;
        
    }
}
