using UnityEngine;

public class OutOfBoundsCleaner : MonoBehaviour
{
    public float LowerBoundY = -50f;

    private void Update()
    {
        if (transform.position.y < LowerBoundY)
        {
            // Destroy(gameObject) ��� ��Ȱ��ȭ�Ͽ� Ǯ�� ��ȯ�� �� �ֵ��� ��
            gameObject.SetActive(false);
        }
    }
}