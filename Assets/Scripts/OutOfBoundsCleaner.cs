using UnityEngine;

public class OutOfBoundsCleaner : MonoBehaviour
{
    public float LowerBoundY = -50f;

    private void Update()
    {
        if (transform.position.y < LowerBoundY)
        {
            // Destroy(gameObject) 대신 비활성화하여 풀에 반환될 수 있도록 함
            gameObject.SetActive(false);
        }
    }
}