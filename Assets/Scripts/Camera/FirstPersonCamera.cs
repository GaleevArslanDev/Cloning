using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float sensitivity = 0.12f;
    [SerializeField] private float verticalLimit = 85f;

    [Header("Head Bob")]
    [SerializeField] private float walkFrequency = 8f;
    [SerializeField] private float runFrequency = 11f;
    [SerializeField] private float walkAmount = 0.035f;
    [SerializeField] private float runAmount = 0.055f;
    [SerializeField] private float bobSmoothness = 10f;

    [Header("Sway")]
    [SerializeField] private float breathingAmount = 0.006f;
    [SerializeField] private float breathingSpeed = 1.4f;

    private Vector3 startPosition;
    private Vector3 currentOffset;
    private float pitch;
    private float bobTime;

    private void Awake()
    {
        if (playerBody == null)
            playerBody = transform.parent;

        if (playerMovement == null && playerBody != null)
            playerMovement = playerBody.GetComponent<PlayerMovement>();

        startPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
        UpdateCameraMotion();
    }

    private void Look()
    {
        if (Mouse.current == null || playerBody == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensitivity;

        pitch = Mathf.Clamp(pitch - mouseDelta.y, -verticalLimit, verticalLimit);
        playerBody.Rotate(Vector3.up * mouseDelta.x);
    }

    private void UpdateCameraMotion()
    {
        Vector3 targetOffset = Vector3.zero;

        if (playerMovement != null && playerMovement.IsMoving)
        {
            float frequency = playerMovement.IsRunning ? runFrequency : walkFrequency;
            float amount = playerMovement.IsRunning ? runAmount : walkAmount;

            bobTime += Time.deltaTime * frequency;
            targetOffset.x = Mathf.Cos(bobTime * 0.5f) * amount * 0.6f;
            targetOffset.y = Mathf.Sin(bobTime) * amount;
        }
        else
        {
            bobTime = 0f;
            targetOffset.y = Mathf.Sin(Time.time * breathingSpeed) * breathingAmount;
        }

        currentOffset = Vector3.Lerp(
            currentOffset,
            targetOffset,
            bobSmoothness * Time.deltaTime
        );

        transform.localPosition = startPosition + currentOffset;
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
