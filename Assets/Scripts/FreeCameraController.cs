using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float fastMoveSpeed = 15f;
    public float lookSensitivity = 2f;

    [Header("Keybinds")]
    public KeyCode upKey = KeyCode.Space;
    public KeyCode downKey = KeyCode.LeftControl;
    public KeyCode fastKey = KeyCode.LeftShift;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        LockCursor();
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
        HandleCursorToggle();
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        Vector3 move = Vector3.zero;
        move += transform.forward * Input.GetAxisRaw("Vertical");   // W/S
        move += transform.right * Input.GetAxisRaw("Horizontal");   // A/D
        if (Input.GetKey(upKey)) move += Vector3.up;
        if (Input.GetKey(downKey)) move += Vector3.down;

        float speed = Input.GetKey(fastKey) ? fastMoveSpeed : moveSpeed;
        transform.position += move.normalized * speed * Time.deltaTime;
    }

    void HandleCursorToggle()
    {
        // 마우스로 다시 잠금
        if (Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }

        // ESC로 해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
