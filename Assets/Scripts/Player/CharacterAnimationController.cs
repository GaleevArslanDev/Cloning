using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform lookSource;
    [SerializeField] private Transform headBone;
    [SerializeField] private float walkingSpeed = 3.5f;
    [SerializeField] private float turningSpeed = 180f;
    [SerializeField] private float turnAnimationSensitivity = 6f;
    [SerializeField] private float movementSmoothness = 10f;
    [SerializeField] private float turnSmoothness = 12f;
    [SerializeField] private float turnStartSmoothness = 35f;
    [SerializeField] private float turnDirectionChangeSmoothness = 7f;
    [SerializeField] private float turnResponsePower = 0.35f;
    [SerializeField] private float turnReleaseDelay = 0.12f;
    [SerializeField] private float movementThreshold = 0.05f;
    [SerializeField] private float turningThreshold = 0.05f;
    [SerializeField] private float jumpVelocityThreshold = 0.8f;
    [SerializeField] private float minimumJumpHeight = 0.2f;
    [SerializeField] private float maximumHeadPitch = 85f;
    [SerializeField] private float maximumHeadYaw = 70f;

    private readonly Dictionary<int, AnimatorControllerParameterType> parameters =
        new Dictionary<int, AnimatorControllerParameterType>();

    private PlayerClimber playerClimber;
    private ClonePlayback clonePlayback;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private Quaternion initialHeadRotation;
    private float currentSpeed;
    private float currentTurn;
    private float heldTurn;
    private float turnReleaseTime;
    private Vector3 verticalMovementStart;
    private float verticalMovementDirection;
    private bool trackingVerticalMovement;
    private bool hasPreviousTransform;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int MovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int TurnParameter = Animator.StringToHash("Turn");
    private static readonly int IdleParameter = Animator.StringToHash("IsIdle");
    private static readonly int JumpingParameter = Animator.StringToHash("IsJumping");

    public Transform HeadBone => headBone;

    private void Awake()
    {
        if (turnAnimationSensitivity <= 0f)
            turnAnimationSensitivity = 6f;
        if (turnStartSmoothness <= 0f)
            turnStartSmoothness = 35f;
        if (turnDirectionChangeSmoothness <= 0f)
            turnDirectionChangeSmoothness = 7f;
        if (turnResponsePower <= 0f)
            turnResponsePower = 0.35f;
        if (minimumJumpHeight <= 0f)
            minimumJumpHeight = 0.2f;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (lookSource == null)
        {
            Camera characterCamera = GetComponentInChildren<Camera>(true);

            if (characterCamera != null)
                lookSource = characterCamera.transform;
            else
                lookSource = FindChild(transform, "CameraRoot");
        }

        if (headBone == null && animator != null && animator.isHuman)
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        if (headBone != null)
            initialHeadRotation = headBone.localRotation;

        playerClimber = GetComponent<PlayerClimber>();
        clonePlayback = GetComponent<ClonePlayback>();
        previousPosition = transform.position;
        previousRotation = transform.rotation;
        verticalMovementStart = transform.position;
        hasPreviousTransform = true;
        CacheParameters();
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        previousRotation = transform.rotation;
        verticalMovementStart = transform.position;
        trackingVerticalMovement = false;
        hasPreviousTransform = true;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (deltaTime <= 0f || !hasPreviousTransform)
        {
            SaveTransform();
            return;
        }

        Vector3 up = Physics.gravity.sqrMagnitude > 0.001f
            ? -Physics.gravity.normalized
            : Vector3.up;
        Vector3 movement = Vector3.ProjectOnPlane(transform.position - previousPosition, up);
        float targetSpeed = movement.magnitude / deltaTime / Mathf.Max(0.01f, walkingSpeed);
        float yaw = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(previousRotation * Vector3.forward, up),
            Vector3.ProjectOnPlane(transform.forward, up),
            up
        );
        float rawTurn = Mathf.Clamp(
            yaw / deltaTime / Mathf.Max(1f, turningSpeed) * turnAnimationSensitivity,
            -1f,
            1f
        );
        float targetTurn = Mathf.Abs(rawTurn) > 0.0001f
            ? Mathf.Sign(rawTurn) * Mathf.Pow(Mathf.Abs(rawTurn), turnResponsePower)
            : 0f;

        if (Mathf.Abs(targetTurn) > turningThreshold)
        {
            heldTurn = targetTurn;
            turnReleaseTime = Mathf.Max(turnReleaseDelay, 0.15f);
            targetTurn = heldTurn;
        }
        else if (turnReleaseTime > 0f)
        {
            turnReleaseTime -= deltaTime;
            targetTurn = heldTurn;
        }
        else
        {
            targetTurn = 0f;
        }

        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            1f - Mathf.Exp(-movementSmoothness * deltaTime)
        );
        bool changingTurnDirection =
            Mathf.Abs(currentTurn) > turningThreshold &&
            Mathf.Abs(targetTurn) > turningThreshold &&
            Mathf.Sign(currentTurn) != Mathf.Sign(targetTurn);
        float activeTurnSmoothness = changingTurnDirection
            ? turnDirectionChangeSmoothness
            : Mathf.Abs(targetTurn) > turningThreshold
                ? turnStartSmoothness
                : turnSmoothness;
        currentTurn = Mathf.Lerp(
            currentTurn,
            targetTurn,
            1f - Mathf.Exp(-activeTurnSmoothness * deltaTime)
        );

        bool isMoving = targetSpeed > movementThreshold;
        bool isJumping = GetIsJumping(deltaTime, up);

        SetFloat(SpeedParameter, currentSpeed);
        SetBool(MovingParameter, isMoving);
        SetFloat(TurnParameter, currentTurn);
        SetBool(IdleParameter, !isMoving && !isJumping && Mathf.Abs(currentTurn) <= turningThreshold);
        SetBool(JumpingParameter, isJumping);
        SaveTransform();
    }

    private void LateUpdate()
    {
        if (headBone == null || lookSource == null)
            return;

        Vector3 localDirection = transform.InverseTransformDirection(lookSource.forward);
        float horizontalLength = new Vector2(localDirection.x, localDirection.z).magnitude;
        float pitch = Mathf.Atan2(-localDirection.y, horizontalLength) * Mathf.Rad2Deg;
        float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

        pitch = Mathf.Clamp(pitch, -maximumHeadPitch, maximumHeadPitch);
        yaw = Mathf.Clamp(yaw, -maximumHeadYaw, maximumHeadYaw);

        headBone.localRotation = initialHeadRotation * Quaternion.Euler(pitch, yaw, 0f);
    }

    private bool GetIsJumping(float deltaTime, Vector3 up)
    {
        bool isTwoPointsCopy = GetComponent<TwoPointsCopy>() != null;

        if (!isTwoPointsCopy)
        {
            if (playerClimber != null)
                return playerClimber.IsJumping;

            if (clonePlayback != null)
                return clonePlayback.IsJumping;
        }

        float verticalMovement = Vector3.Dot(transform.position - previousPosition, up);
        float verticalSpeed = verticalMovement / deltaTime;

        if (Mathf.Abs(verticalSpeed) < jumpVelocityThreshold)
        {
            trackingVerticalMovement = false;
            return false;
        }

        float direction = Mathf.Sign(verticalMovement);

        if (!trackingVerticalMovement || direction != verticalMovementDirection)
        {
            verticalMovementStart = previousPosition;
            verticalMovementDirection = direction;
            trackingVerticalMovement = true;
        }

        float height = Mathf.Abs(
            Vector3.Dot(transform.position - verticalMovementStart, up)
        );
        return height >= minimumJumpHeight;
    }

    private void CacheParameters()
    {
        parameters.Clear();

        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            parameters[parameter.nameHash] = parameter.type;
    }

    private void SetFloat(int parameter, float value)
    {
        if (animator != null &&
            parameters.TryGetValue(parameter, out AnimatorControllerParameterType type) &&
            type == AnimatorControllerParameterType.Float)
        {
            animator.SetFloat(parameter, value);
        }
    }

    private void SetBool(int parameter, bool value)
    {
        if (animator != null &&
            parameters.TryGetValue(parameter, out AnimatorControllerParameterType type) &&
            type == AnimatorControllerParameterType.Bool)
        {
            animator.SetBool(parameter, value);
        }
    }

    private void SaveTransform()
    {
        previousPosition = transform.position;
        previousRotation = transform.rotation;
        hasPreviousTransform = true;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }
}
