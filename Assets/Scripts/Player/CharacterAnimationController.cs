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
    [SerializeField] private float movementSmoothness = 10f;
    [SerializeField] private float turnSmoothness = 12f;
    [SerializeField] private float movementThreshold = 0.05f;
    [SerializeField] private float turningThreshold = 0.05f;
    [SerializeField] private float jumpVelocityThreshold = 0.8f;
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
    private bool hasPreviousTransform;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int MovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int TurnParameter = Animator.StringToHash("Turn");
    private static readonly int IdleParameter = Animator.StringToHash("IsIdle");
    private static readonly int JumpingParameter = Animator.StringToHash("IsJumping");

    private void Awake()
    {
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
        hasPreviousTransform = true;
        CacheParameters();
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        previousRotation = transform.rotation;
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
        float targetTurn = yaw / deltaTime / Mathf.Max(1f, turningSpeed);

        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            1f - Mathf.Exp(-movementSmoothness * deltaTime)
        );
        currentTurn = Mathf.Lerp(
            currentTurn,
            targetTurn,
            1f - Mathf.Exp(-turnSmoothness * deltaTime)
        );

        bool isMoving = targetSpeed > movementThreshold;
        bool isJumping = GetIsJumping(deltaTime, up);

        SetFloat(SpeedParameter, currentSpeed);
        SetBool(MovingParameter, isMoving);
        SetFloat(TurnParameter, currentTurn);
        SetBool(IdleParameter, !isMoving && !isJumping && Mathf.Abs(targetTurn) <= turningThreshold);
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
        if (playerClimber != null && playerClimber.IsJumping)
            return true;

        if (clonePlayback != null)
            return clonePlayback.IsJumping;

        float verticalSpeed = Vector3.Dot(transform.position - previousPosition, up) / deltaTime;
        return Mathf.Abs(verticalSpeed) >= jumpVelocityThreshold;
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
