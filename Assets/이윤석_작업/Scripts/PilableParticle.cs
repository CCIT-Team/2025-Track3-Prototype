using System.Collections;
using UnityEngine;

public class PilableParticle : MonoBehaviour, IPoolable
{
    [SerializeField]
    private TerrainLayer _layer;
    [SerializeField]
    private float _waitTime;

    private GameObjectPool _pool = null;

    private bool _stopUpdate = false;

    [SerializeField]
    private float _power;

    public void SetTerrainLayer(TerrainLayer layer)
    {
        _layer = layer;
    }

    private IEnumerator Pile(TerrainHeightRaiser raiser)
    {
        if (_pool == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(_waitTime);
        raiser.PileTerrain(transform.position, _layer, 5f, transform.localScale.y / _power);
        _pool.ReturnGameObject(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var raiser = collision.gameObject.GetComponent<TerrainHeightRaiser>();
        if (raiser == null)
        {
            return;
        }
        StartCoroutine(Pile(raiser));
    }

    private void Update()
    {
        Vector3 terrainPos = Terrain.activeTerrain.transform.position;

        if (!_stopUpdate && transform.position.y < terrainPos.y)
        {
            StartCoroutine(Pile(Terrain.activeTerrain.gameObject.GetComponent<TerrainHeightRaiser>()));
            _stopUpdate = true;
        }
    }

    void OnEnable()
    {
        _stopUpdate = false;
    }

    public void SetPoolInstance(GameObjectPool poolInstance)
    {
        _pool = poolInstance;
    }

    public bool ComparePoolInstance(GameObjectPool poolInstance)
    {
        return _pool == poolInstance;
    }
}
