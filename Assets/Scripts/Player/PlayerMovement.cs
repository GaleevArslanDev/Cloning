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
    private AudioSource footstepAudioSource;
    private AudioSource fallAudioSource;
    private AudioClip[] footstepSounds;
    private AudioClip fallSound;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.35f;
    [SerializeField] private float fallSoundDelay;
    [SerializeField] private float groundedSoundGrace = 0.15f;
    [SerializeField] private float minimumFallSpeed = 0.5f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private Vector2 moveInput;
    private Rigidbody groundPlatform;
    private Vector3 groundPlatformPosition;
    private bool groundContactFound;
    private float stepTimer;
    private float fallTimer;
    private bool fallSoundPlayed;
    private bool stepSequenceActive;
    private float timeSinceGrounded;
    private Vector3 previousSoundPosition;

    public bool IsMoving => moveInput.sqrMagnitude > 0.01f && controller.isGrounded;
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        footstepAudioSource = PlayerAudioController.CreatePlayerSource();
        fallAudioSource = PlayerAudioController.CreatePlayerSource();
        GameAudioClips audioClips = GameAudioClips.Instance;

        if (audioClips != null)
        {
            footstepSounds = audioClips.Footsteps;
            fallSound = audioClips.Falling;
        }

        controller = GetComponent<CharacterController>();
        previousSoundPosition = transform.position;

        if (fallSoundDelay <= 0f)
            fallSoundDelay = 0.12f;

        if (GetComponent<CharacterAnimationController>() == null)
            gameObject.AddComponent<CharacterAnimationController>();
    }

    private void OnEnable()
    {
        previousSoundPosition = transform.position;
        timeSinceGrounded = 0f;
        fallTimer = 0f;
        fallSoundPlayed = false;
        stepTimer = 0f;
        stepSequenceActive = false;
    }

    private void OnDisable()
    {
        if (footstepAudioSource != null)
            footstepAudioSource.Stop();

        ClearGroundPlatform();
    }

    private void Update()
    {
        if (PauseMenuController.IsGamePaused) return;

        groundContactFound = false;
        ApplyPlatformMovement();
        ReadInput();
        Move();
        Vector3 soundUp = Physics.gravity.sqrMagnitude > 0.001f
            ? -Physics.gravity.normalized
            : Vector3.up;
        float verticalSpeed = Time.deltaTime > 0f
            ? Vector3.Dot(transform.position - previousSoundPosition, soundUp) / Time.deltaTime
            : 0f;
        previousSoundPosition = transform.position;
        UpdateMovementSounds(verticalSpeed);

        if (!groundContactFound && !controller.isGrounded)
            ClearGroundPlatform();

        if (groundPlatform != null)
            groundPlatformPosition = groundPlatform.transform.position;
    }

    private void UpdateMovementSounds(float actualVerticalSpeed)
    {
        bool groundedNow = controller.isGrounded || groundContactFound;

        if (groundedNow)
            timeSinceGrounded = 0f;
        else
            timeSinceGrounded += Time.deltaTime;

        bool stableGrounded = timeSinceGrounded <= groundedSoundGrace;
        bool shouldPlaySteps = stableGrounded &&
                               moveInput.sqrMagnitude > 0.01f &&
                               horizontalVelocity.sqrMagnitude > 0.1f;

        if (shouldPlaySteps)
        {
            if (!stepSequenceActive)
            {
                stepSequenceActive = true;
                stepTimer = GetStepInterval() * 0.5f;
            }

            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayRandomFootstep();
                stepTimer = GetStepInterval();
            }
        }
        else
        {
            stepTimer = 0f;
            stepSequenceActive = false;
        }

        if (groundedNow)
        {
            fallTimer = 0f;
            fallSoundPlayed = false;
        }
        else if (!stableGrounded && actualVerticalSpeed < -minimumFallSpeed)
        {
            fallTimer += Time.deltaTime;

            if (!fallSoundPlayed && fallTimer >= fallSoundDelay)
            {
                PlaySound(fallAudioSource, fallSound);
                fallSoundPlayed = true;
            }
        }
        else
        {
            fallTimer = 0f;
        }
    }

    private float GetStepInterval()
    {
        return Mathf.Max(0.05f, IsRunning ? runStepInterval : walkStepInterval);
    }

    private void PlayRandomFootstep()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
            return;

        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            return;

        PlaySound(
            footstepAudioSource,
            footstepSounds[Random.Range(0, footstepSounds.Length)]
        );
    }

    private void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip);
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
        stepTimer = 0f;
        fallTimer = 0f;
        fallSoundPlayed = false;
        stepSequenceActive = false;
        timeSinceGrounded = 0f;
        previousSoundPosition = transform.position;
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
