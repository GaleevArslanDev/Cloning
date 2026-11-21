using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float acceleration = 14f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private Vector2 moveInput;
    private Rigidbody groundPlatform;
    private Vector3 groundPlatformPosition;
    private bool groundContactFound;

    public bool IsMoving => moveInput.sqrMagnitude > 0.01f && controller.isGrounded;
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (GetComponent<CharacterAnimationController>() == null)
            gameObject.AddComponent<CharacterAnimationController>();
    }

    private void OnDisable()
    {
        ClearGroundPlatform();
    }

    private void Update()
    {
        if (PauseMenuController.IsGamePaused) return;

        ApplyPlatformMovement();
        groundContactFound = false;
        ReadInput();
        Move();

        if (!groundContactFound && !controller.isGrounded)
            ClearGroundPlatform();

        if (groundPlatform != null)
            groundPlatformPosition = groundPlatform.transform.position;
    }

    private void ApplyPlatformMovement()
    {
        if (groundPlatform == null)
            return;

        Vector3 platformPosition = groundPlatform.transform.position;
        Vector3 movement = platformPosition - groundPlatformPosition;

        if (movement.sqrMagnitude > 0.0000001f)
            controller.Move(movement);

        groundPlatformPosition = platformPosition;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Vector3.Dot(hit.normal, Vector3.up) < 0.5f)
            return;

        groundContactFound = true;
        Rigidbody hitBody = hit.rigidbody;

        if (hitBody == null || !hitBody.isKinematic)
        {
            ClearGroundPlatform();
            return;
        }

        if (groundPlatform == hitBody)
            return;

        groundPlatform = hitBody;
        groundPlatformPosition = hitBody.transform.position;
    }

    private void ClearGroundPlatform()
    {
        groundPlatform = null;
        groundPlatformPosition = Vector3.zero;
    }

    private void ReadInput()
    {
        bool movingWithMouse = Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool movingWithKeyboard = Keyboard.current != null && Keyboard.current.wKey.isPressed;

        moveInput = movingWithMouse || movingWithKeyboard ? Vector2.up : Vector2.zero;
        IsRunning = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && moveInput.y > 0f;
    }

    private void Move()
    {
        float targetSpeed = IsRunning ? runSpeed : walkSpeed;
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = direction * targetSpeed;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.deltaTime
        );

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;

        horizontalVelocity = Vector3.zero;
        verticalVelocity = -2f;
        moveInput = Vector2.zero;
        IsRunning = false;
        ClearGroundPlatform();
    }

    public bool MoveTo(Vector3 targetPosition, float speed, float stopDistance)
    {
        Vector3 difference = targetPosition - transform.position;
        float distance = difference.magnitude;

        if (distance <= stopDistance)
            return true;

        horizontalVelocity = Vector3.zero;
        verticalVelocity = -2f;
        moveInput = Vector2.zero;
        IsRunning = false;

        float movement = Mathf.Min(
            distance,
            Mathf.Max(0f, speed) * Time.deltaTime
        );
        controller.Move(difference.normalized * movement);
        return Vector3.Distance(transform.position, targetPosition) <= stopDistance;
    }
}
