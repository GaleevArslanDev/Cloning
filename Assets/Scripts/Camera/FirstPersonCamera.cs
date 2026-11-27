using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(200)]
public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField, Range(0f, 5f)] private float sensitivity = 0.12f;
    [SerializeField, Range(0f, 89f)] private float verticalLimit = 85f;

    [Header("Animated Body")]
    [SerializeField] private Transform animatedHead;
    [SerializeField] private bool followAnimatedHead = true;
    [SerializeField, Min(0f)] private float animationSmoothness = 14f;
    [SerializeField, Min(0f)] private float maximumHeadFollowLag = 0.015f;

    [Header("Head Bob")]
    [SerializeField, Min(0f)] private float walkFrequency = 8f;
    [SerializeField, Min(0f)] private float runFrequency = 11f;
    [SerializeField, Min(0f)] private float walkAmount = 0.035f;
    [SerializeField, Min(0f)] private float runAmount = 0.055f;
    [SerializeField, Min(0f)] private float bobSmoothness = 10f;

    [Header("Sway")]
    [SerializeField, Min(0f)] private float breathingAmount = 0.006f;
    [SerializeField, Min(0f)] private float breathingSpeed = 1.4f;

    private Vector3 startPosition;
    private Vector3 currentOffset;
    private Vector3 cameraPositionInHeadSpace;
    private Vector3 animatedCameraPosition;
    private float pitch;
    private float bobTime;
    private bool animatedHeadReferenceReady;
    private bool wasPaused;

    private void Awake()
    {
        sensitivity = Mathf.Clamp(SaveDataValidator.Finite(sensitivity, 0.12f), 0f, 5f);
        verticalLimit = Mathf.Clamp(SaveDataValidator.Finite(verticalLimit, 85f), 0f, 89f);
        animationSmoothness = Mathf.Max(
            0f,
            SaveDataValidator.Finite(animationSmoothness, 14f)
        );

        if (maximumHeadFollowLag <= 0f)
            maximumHeadFollowLag = 0.015f;

        if (playerBody == null)
            playerBody = transform.parent;

        if (playerMovement == null && playerBody != null)
            playerMovement = playerBody.GetComponent<PlayerMovement>();

        startPosition = transform.localPosition;
        FindAnimatedHead();
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (animatedHeadReferenceReady)
            SnapToAnimatedHead();
        else
            animatedCameraPosition = transform.localPosition;
    }

    private void Update()
    {
        if (PauseMenuController.IsGamePaused)
        {
            wasPaused = true;
            return;
        }
        // Relocking the cursor can produce a synthetic mouse delta.
        if (wasPaused)
        {
            wasPaused = false;
            return;
        }

        Look();
    }

    private void LateUpdate()
    {
        if (PauseMenuController.IsGamePaused) return;

        UpdateCameraMotion();
    }

    private void Look()
    {
        if (Mouse.current == null || playerBody == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensitivity;

        pitch = Mathf.Clamp(pitch - mouseDelta.y, -verticalLimit, verticalLimit);
        playerBody.Rotate(Vector3.up * mouseDelta.x);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateCameraMotion()
    {
        Vector3 targetOffset = Vector3.zero;

        if (followAnimatedHead && animatedHead != null && playerBody != null)
        {
            if (!animatedHeadReferenceReady)
            {
                cameraPositionInHeadSpace = animatedHead.InverseTransformPoint(transform.position);
                animatedCameraPosition = transform.localPosition;
                animatedHeadReferenceReady = true;
            }

            Vector3 targetPosition = GetAnimatedCameraPosition();
            float follow = 1f - Mathf.Exp(-animationSmoothness * Time.deltaTime);
            animatedCameraPosition = Vector3.Lerp(
                animatedCameraPosition,
                targetPosition,
                follow
            );

            Vector3 followLag = animatedCameraPosition - targetPosition;

            if (maximumHeadFollowLag > 0f &&
                followLag.magnitude > maximumHeadFollowLag)
            {
                Vector3 nearestSmoothPosition = targetPosition
                    + followLag.normalized * maximumHeadFollowLag;
                float catchUp = 1f - Mathf.Exp(
                    -animationSmoothness * 0.5f * Time.deltaTime
                );
                animatedCameraPosition = Vector3.Lerp(
                    animatedCameraPosition,
                    nearestSmoothPosition,
                    catchUp
                );
            }
        }
        else if (playerMovement != null && playerMovement.IsMoving)
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

        Vector3 basePosition = followAnimatedHead && animatedHead != null
            ? animatedCameraPosition
            : startPosition;
        transform.localPosition = basePosition + currentOffset;
    }

    public void ResetLook()
    {
        pitch = 0f;
        bobTime = 0f;
        currentOffset = Vector3.zero;
        SnapToAnimatedHead();
        transform.localRotation = Quaternion.identity;
    }

    public Quaternion BodyRotation => playerBody != null
        ? playerBody.rotation
        : Quaternion.identity;

    public float Pitch => pitch;

    public void SetLook(Quaternion bodyRotation, float newPitch)
    {
        if (playerBody != null)
            playerBody.rotation = SaveDataValidator.Rotation(bodyRotation);

        pitch = Mathf.Clamp(
            SaveDataValidator.Finite(newPitch, 0f),
            -verticalLimit,
            verticalLimit
        );
        bobTime = 0f;
        currentOffset = Vector3.zero;
        SnapToAnimatedHead();
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private Vector3 GetAnimatedCameraPosition()
    {
        return playerBody.InverseTransformPoint(
            animatedHead.TransformPoint(cameraPositionInHeadSpace)
        );
    }

    private void SnapToAnimatedHead()
    {
        if (animatedHeadReferenceReady && animatedHead != null && playerBody != null)
            animatedCameraPosition = GetAnimatedCameraPosition();
        else
            animatedCameraPosition = startPosition;

        transform.localPosition = animatedCameraPosition;
    }

    private void FindAnimatedHead()
    {
        if (animatedHead != null || playerBody == null)
            return;

        CharacterAnimationController animationController =
            playerBody.GetComponent<CharacterAnimationController>();

        if (animationController != null && animationController.HeadBone != null)
        {
            animatedHead = animationController.HeadBone;
            return;
        }

        foreach (Transform child in playerBody.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "mixamorig:Head" || child.name == "Head")
            {
                animatedHead = child;
                return;
            }
        }
    }

}
