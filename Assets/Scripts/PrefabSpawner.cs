using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [Header("배치할 오브젝트 목록")]
    // 여러 개의 프리팹을 담을 수 있도록 배열(array)로 변경합니다.
    public GameObject[] objectPrefabs;

    [Header("배치 설정")]
    public int spawnCount = 100;
    public Vector3 spawnAreaSize = new Vector3(200, 0, 200);

    [Header("레이캐스트 설정")]
    public LayerMask groundLayer;

    [Header("생성 옵션")]
    public Transform parentObject;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    [ContextMenu("Spawn Objects")]
    void SpawnObjects()
    {
        if (parentObject == null)
        {
            parentObject = this.transform;
        }

        // 배치할 프리팹 목록이 비어있는지 확인합니다.
        if (objectPrefabs == null || objectPrefabs.Length == 0)
        {
            Debug.LogError("배치할 오브젝트 프리팹이 목록에 없습니다! Inspector 창에서 설정해주세요.");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            float randomX = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
            float randomZ = Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2);
            Vector3 spawnOrigin = this.transform.position + new Vector3(randomX, 1000f, randomZ);

            RaycastHit hit;
            if (Physics.Raycast(spawnOrigin, Vector3.down, out hit, 2000f, groundLayer))
            {
                // *** 핵심 변경사항: 목록에서 무작위 프리팹 선택 ***
                int randomIndex = Random.Range(0, objectPrefabs.Length);
                GameObject prefabToSpawn = objectPrefabs[randomIndex];
                // ***********************************************

                // 만약 선택된 프리팹이 비어있다면(의도적인 빈 공간), 그냥 건너뜁니다.
                if (prefabToSpawn == null)
                {
                    continue;
                }

                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                GameObject spawnedObject = Instantiate(prefabToSpawn, hit.point, randomRotation, parentObject);

                float randomScale = Random.Range(minScale, maxScale);
                spawnedObject.transform.localScale = Vector3.one * randomScale;
            }
        }

        Debug.Log(spawnCount + "번의 시도 후, 오브젝트 배치가 완료되었습니다!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawCube(this.transform.position, new Vector3(spawnAreaSize.x, 2, spawnAreaSize.z));
    }
}