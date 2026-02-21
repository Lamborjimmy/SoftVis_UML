using UnityEngine;
using UnityEngine.EventSystems;

public class FlythroughCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float boostMultiplier = 5f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;

    [Header("Speed Control")]
    [SerializeField] private float minMoveSpeed = 1f;
    [SerializeField] private float maxMoveSpeed = 100f;

    [Header("Toggle Keys")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private KeyCode uiModeKey = KeyCode.Escape;

    private float currentMoveSpeed;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private bool flyEnabled = true;

    private void Start()
    {
        currentMoveSpeed = moveSpeed;
        flyEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleToggle();
        if (flyEnabled)
        {
            HandleMouseLook();
            HandleMovement();
            HandleSpeedChange();
        }
    }

    private void HandleToggle()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            flyEnabled = !flyEnabled;
            Cursor.lockState = flyEnabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !flyEnabled;
        }

        if (Input.GetKeyDown(uiModeKey))
        {
            flyEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (!flyEnabled && Cursor.visible && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                flyEnabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? -1f : 1f);

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }

    private void HandleMovement()
    {
        float speed = currentMoveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            speed *= boostMultiplier;
        }

        Vector3 moveDir = Vector3.zero;
        moveDir += transform.forward * Input.GetAxisRaw("Vertical");
        moveDir += transform.right * Input.GetAxisRaw("Horizontal");

        float verticalInput = 0f;
        if (Input.GetKey(KeyCode.E)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.Q)) verticalInput -= 1f;
        moveDir += transform.up * verticalInput;

        if (moveDir.sqrMagnitude > 0f)
        {
            moveDir.Normalize();
        }

        transform.position += moveDir * speed * Time.deltaTime;
    }

    private void HandleSpeedChange()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            currentMoveSpeed = Mathf.Min(currentMoveSpeed * 2f, maxMoveSpeed);
        }
        else if (scroll < 0f)
        {
            currentMoveSpeed = Mathf.Max(currentMoveSpeed * 0.5f, minMoveSpeed);
        }
    }
}