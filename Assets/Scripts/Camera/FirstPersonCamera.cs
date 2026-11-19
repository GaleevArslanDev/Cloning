using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float sensitivity = 0.12f;
    [SerializeField] private float verticalLimit = 85f;

    [Header("Animated Body")]
    [SerializeField] private Transform animatedHead;
    [SerializeField] private bool followAnimatedHead = true;
    [SerializeField] private float animationSmoothness = 14f;
    [SerializeField] private float maximumHeadFollowLag = 0.015f;

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
    private Vector3 cameraPositionInHeadSpace;
    private Vector3 animatedCameraPosition;
    private float pitch;
    private float bobTime;
    private bool animatedHeadReferenceReady;

    private void Awake()
    {
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
        Look();
    }

    private void LateUpdate()
    {
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
            animatedCameraPosition = Vector3.Lerp(
                animatedCameraPosition,
                targetPosition,
                1f - Mathf.Exp(-animationSmoothness * Time.deltaTime)
            );

            Vector3 followLag = animatedCameraPosition - targetPosition;

            if (followLag.magnitude > maximumHeadFollowLag)
            {
                animatedCameraPosition = targetPosition
                    + followLag.normalized * maximumHeadFollowLag;
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
            playerBody.rotation = bodyRotation;

        pitch = Mathf.Clamp(newPitch, -verticalLimit, verticalLimit);
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
