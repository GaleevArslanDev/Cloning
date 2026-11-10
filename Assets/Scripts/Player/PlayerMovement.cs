using UnityEngine;
using UnityEngine.InputSystem;

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

    public bool IsMoving => moveInput.sqrMagnitude > 0.01f && controller.isGrounded;
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ReadInput();
        Move();
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
}
