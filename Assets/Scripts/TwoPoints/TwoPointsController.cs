using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TwoPointsController : MonoBehaviour
{
    [SerializeField] private Transform lowerStartPoint;
    [SerializeField] private Transform upperStartPoint;
    [SerializeField] private Transform lowerHead;
    [SerializeField] private GameObject upperPlayerPrefab;
    [SerializeField] private GameObject upperClonePrefab;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private float collisionOffset = 0.02f;
    [SerializeField] private float copyMoveSpeed = 6f;
    [SerializeField] private float upperGravity = 20f;
    [SerializeField] private float rescueDistance = 1.25f;
    [SerializeField] private float cloneRefreshInterval = 0.1f;

    private readonly Dictionary<ClonePlayback, GameObject> cloneCopies = new Dictionary<ClonePlayback, GameObject>();
    private readonly List<ClonePlayback> removedClones = new List<ClonePlayback>();
    private readonly HashSet<GameObject> stuckCopies = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, float> copyHeightOffsets = new Dictionary<GameObject, float>();
    private readonly Dictionary<GameObject, float> copyGravitySpeeds = new Dictionary<GameObject, float>();
    private GameObject upperPlayer;
    private Transform upperHead;
    private TwoPointsUpperInteractor upperPlayerInteractor;
    private float nextCloneRefreshTime;

    private Vector3 WorldUp => Physics.gravity.sqrMagnitude > 0.001f
        ? -Physics.gravity.normalized
        : Vector3.up;

    private void Awake()
    {
        if (lowerHead == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera != null)
                lowerHead = playerCamera.transform;
        }
    }

    private void OnEnable()
    {
        CreateUpperPlayer();
        PlaceCopy(transform, upperPlayer);
        SyncUpperPlayerHead();
        nextCloneRefreshTime = 0f;
    }

    private void LateUpdate()
    {
        SyncUpperPlayer();
        SyncClones();
    }

    private void Update()
    {
        if (upperPlayerInteractor == null || Mouse.current == null)
            return;

        upperPlayerInteractor.ProcessInput(
            Mouse.current.leftButton.isPressed,
            Mouse.current.rightButton.wasPressedThisFrame,
            Mouse.current.scroll.ReadValue().y,
            Time.deltaTime
        );
    }

    public void Activate()
    {
        enabled = true;
    }

    public void Deactivate()
    {
        enabled = false;
    }

    private float GetAllowedDistance(GameObject copy, Vector3 direction, float distance)
    {
        if (!GetCapsule(copy, out Vector3 pointA, out Vector3 pointB, out float radius))
            return distance;

        RaycastHit[] hits = Physics.CapsuleCastAll(
            pointA,
            pointB,
            radius,
            direction,
            distance + collisionOffset,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        float allowedDistance = distance;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.GetComponentInParent<TwoPointsCopy>() != null)
                continue;

            allowedDistance = Mathf.Min(
                allowedDistance,
                Mathf.Max(0f, hit.distance - collisionOffset)
            );
        }

        return allowedDistance;
    }

    private bool GetCapsule(
        GameObject copy,
        out Vector3 pointA,
        out Vector3 pointB,
        out float radius
    )
    {
        CharacterController characterController = copy.GetComponent<CharacterController>();
        CapsuleCollider capsuleCollider = copy.GetComponent<CapsuleCollider>();

        if (characterController == null && capsuleCollider == null)
        {
            pointA = copy.transform.position;
            pointB = copy.transform.position;
            radius = 0f;
            return false;
        }

        Transform controllerTransform = characterController != null
            ? characterController.transform
            : capsuleCollider.transform;
        Vector3 scale = controllerTransform.lossyScale;
        Vector3 axis = controllerTransform.up;
        float axisScale = Mathf.Abs(scale.y);
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float controllerRadius;
        float controllerHeight;
        Vector3 controllerCenter;

        if (characterController != null)
        {
            controllerRadius = characterController.radius;
            controllerHeight = characterController.height;
            controllerCenter = characterController.center;
        }
        else
        {
            controllerRadius = capsuleCollider.radius;
            controllerHeight = capsuleCollider.height;
            controllerCenter = capsuleCollider.center;

            if (capsuleCollider.direction == 0)
            {
                axis = controllerTransform.right;
                axisScale = Mathf.Abs(scale.x);
                radiusScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            }
            else if (capsuleCollider.direction == 2)
            {
                axis = controllerTransform.forward;
                axisScale = Mathf.Abs(scale.z);
                radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            }
        }

        radius = controllerRadius * radiusScale;
        float height = Mathf.Max(controllerHeight * axisScale, radius * 2f);
        float halfLine = Mathf.Max(0f, height * 0.5f - radius);
        Vector3 center = controllerTransform.TransformPoint(controllerCenter);

        pointA = center + axis.normalized * halfLine;
        pointB = center - axis.normalized * halfLine;
        return true;
    }

    private void CreateUpperPlayer()
    {
        if (upperPlayer != null || upperPlayerPrefab == null)
            return;

        upperPlayer = Instantiate(upperPlayerPrefab);
        upperPlayer.name = "Upper Player";

        if (upperPlayer.GetComponent<TwoPointsCopy>() == null)
            upperPlayer.AddComponent<TwoPointsCopy>();

        Camera upperCamera = upperPlayer.GetComponentInChildren<Camera>();

        if (upperCamera != null)
        {
            upperHead = upperCamera.transform;
            upperCamera.enabled = false;
        }

        if (upperHead == null)
            upperHead = FindChild(upperPlayer.transform, "CameraRoot");

        upperPlayerInteractor = ConfigureInteractor(upperPlayer, upperHead);
    }

    private void SyncUpperPlayer()
    {
        if (upperPlayer == null)
            return;

        MoveCopy(transform, upperPlayer);
        SyncUpperPlayerHead();
    }

    private void SyncUpperPlayerHead()
    {
        SyncHead(lowerHead, upperHead);
    }

    private void SyncClones()
    {
        if (Time.unscaledTime >= nextCloneRefreshTime)
        {
            RefreshClones();
            nextCloneRefreshTime = Time.unscaledTime + Mathf.Max(0.02f, cloneRefreshInterval);
        }

        foreach (KeyValuePair<ClonePlayback, GameObject> pair in cloneCopies)
        {
            if (pair.Key == null || pair.Value == null || !pair.Key.gameObject.activeInHierarchy)
                continue;

            MoveCopy(pair.Key.transform, pair.Value);
            SyncHead(
                FindChild(pair.Key.transform, "CameraRoot"),
                FindChild(pair.Value.transform, "CameraRoot")
            );

            TwoPointsUpperInteractor interactor = pair.Value.GetComponent<TwoPointsUpperInteractor>();

            if (interactor != null)
            {
                interactor.ProcessInput(
                    pair.Key.IsLeftInputHeld,
                    pair.Key.RightInputPressedThisFrame,
                    pair.Key.ScrollInputThisFrame,
                    Time.deltaTime
                );
            }
        }
    }

    private void RefreshClones()
    {
        ClonePlayback[] clones = FindObjectsByType<ClonePlayback>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (ClonePlayback clone in clones)
        {
            if (clone == null || clone.GetComponent<TwoPointsCopy>() != null)
                continue;

            if (!cloneCopies.TryGetValue(clone, out GameObject copy) || copy == null)
            {
                copy = CreateCloneCopy();

                if (copy == null)
                    continue;

                cloneCopies[clone] = copy;
                PlaceCopy(clone.transform, copy);
            }
        }

        removedClones.Clear();

        foreach (KeyValuePair<ClonePlayback, GameObject> pair in cloneCopies)
        {
            if (pair.Key != null && pair.Key.gameObject.activeInHierarchy)
                continue;

            if (pair.Value != null)
            {
                stuckCopies.Remove(pair.Value);
                copyHeightOffsets.Remove(pair.Value);
                copyGravitySpeeds.Remove(pair.Value);
                Destroy(pair.Value);
            }

            removedClones.Add(pair.Key);
        }

        foreach (ClonePlayback clone in removedClones)
            cloneCopies.Remove(clone);
    }

    private GameObject CreateCloneCopy()
    {
        GameObject prefab = upperClonePrefab != null ? upperClonePrefab : upperPlayerPrefab;

        if (prefab == null)
            return null;

        GameObject copy = Instantiate(prefab);
        copy.name = "Upper Clone";

        if (copy.GetComponent<TwoPointsCopy>() == null)
            copy.AddComponent<TwoPointsCopy>();

        Camera copyCamera = copy.GetComponentInChildren<Camera>();

        if (copyCamera != null)
            copyCamera.enabled = false;

        Transform copyHead = FindChild(copy.transform, "CameraRoot");
        ConfigureInteractor(copy, copyHead);

        return copy;
    }

    private TwoPointsUpperInteractor ConfigureInteractor(GameObject copy, Transform viewPoint)
    {
        TwoPointsUpperInteractor interactor = copy.GetComponent<TwoPointsUpperInteractor>();
        PlayerCarry playerCarry = copy.GetComponent<PlayerCarry>();

        if (interactor == null)
            interactor = copy.AddComponent<TwoPointsUpperInteractor>();

        if (playerCarry == null)
            playerCarry = copy.AddComponent<PlayerCarry>();

        playerCarry.ConfigureExternalInput(viewPoint, WorldUp);

        interactor.Configure(
            viewPoint,
            interactionDistance,
            interactionLayers,
            -WorldUp,
            position => SetCopyHeight(copy, position),
            playerCarry
        );

        return interactor;
    }

    private void SetCopyHeight(GameObject copy, Vector3 landingPosition)
    {
        Vector3 upperOrigin = upperStartPoint != null
            ? upperStartPoint.position
            : transform.position;

        copyHeightOffsets[copy] = Mathf.Min(
            0f,
            Vector3.Dot(landingPosition - upperOrigin, WorldUp)
        );
        copyGravitySpeeds[copy] = 0f;
    }

    private void MoveCopy(Transform source, GameObject copy)
    {
        GetTargetPose(source, copy, out Vector3 targetPosition, out Quaternion targetRotation);

        Vector3 difference = targetPosition - copy.transform.position;
        Vector3 horizontalMovement = Vector3.ProjectOnPlane(difference, WorldUp);
        float targetDistance = horizontalMovement.magnitude;
        float maximumMovement = Mathf.Max(0f, copyMoveSpeed) * Time.deltaTime;
        Vector3 position = copy.transform.position;
        UpdateVerticalMovement(copy, ref position, maximumMovement);

        if (stuckCopies.Contains(copy))
        {
            if (targetDistance > Mathf.Max(0f, rescueDistance))
            {
                SetCopyPose(
                    copy.transform,
                    position,
                    targetRotation
                );
                return;
            }

            stuckCopies.Remove(copy);
        }

        float distance = Mathf.Min(
            targetDistance,
            maximumMovement
        );

        if (distance > 0.0001f)
        {
            Vector3 direction = horizontalMovement.normalized;
            float allowedDistance = GetAllowedDistance(copy, direction, distance);
            position += direction * allowedDistance;

            if (allowedDistance + 0.001f < distance)
                stuckCopies.Add(copy);
        }

        SetCopyPose(copy.transform, position, targetRotation);
    }

    private void UpdateVerticalMovement(
        GameObject copy,
        ref Vector3 position,
        float maximumMovement
    )
    {
        if (copyHeightOffsets.TryGetValue(copy, out float heightOffset))
        {
            Vector3 verticalTarget = GetUpperOrigin() + WorldUp * heightOffset;
            Vector3 verticalMovement = Vector3.Project(
                verticalTarget - copy.transform.position,
                WorldUp
            );
            float distance = Mathf.Min(verticalMovement.magnitude, maximumMovement);

            if (distance <= 0.001f)
            {
                copyHeightOffsets.Remove(copy);
                return;
            }

            Vector3 direction = verticalMovement.normalized;
            float allowedDistance = GetAllowedDistance(copy, direction, distance);
            position += direction * allowedDistance;

            if (allowedDistance + 0.001f < distance ||
                verticalMovement.magnitude <= maximumMovement + 0.001f)
            {
                copyHeightOffsets.Remove(copy);
            }

            return;
        }

        float gravitySpeed = copyGravitySpeeds.TryGetValue(copy, out float savedSpeed)
            ? savedSpeed
            : 0f;

        gravitySpeed += Mathf.Max(0f, upperGravity) * Time.deltaTime;
        float gravityDistance = gravitySpeed * Time.deltaTime;
        float allowedGravityDistance = GetAllowedDistance(copy, WorldUp, gravityDistance);
        position += WorldUp * allowedGravityDistance;

        if (allowedGravityDistance + 0.001f < gravityDistance)
            gravitySpeed = 0f;

        copyGravitySpeeds[copy] = gravitySpeed;
    }

    private void PlaceCopy(Transform source, GameObject copy)
    {
        if (source == null || copy == null)
            return;

        GetTargetPose(source, copy, out Vector3 targetPosition, out Quaternion targetRotation);
        SetCopyPose(copy.transform, targetPosition, targetRotation);
    }

    private void GetTargetPose(
        Transform source,
        GameObject copy,
        out Vector3 targetPosition,
        out Quaternion targetRotation
    )
    {
        Vector3 lowerOrigin = lowerStartPoint != null ? lowerStartPoint.position : transform.position;
        Vector3 upperOrigin = GetUpperOrigin();
        Vector3 offset = source.position - lowerOrigin;
        Vector3 horizontalOffset = Vector3.ProjectOnPlane(offset, WorldUp);
        float currentHeight = Vector3.Dot(copy.transform.position - upperOrigin, WorldUp);

        targetPosition = upperOrigin + horizontalOffset + WorldUp * currentHeight;
        targetRotation = Quaternion.LookRotation(source.forward, -WorldUp);
    }

    private Vector3 GetUpperOrigin()
    {
        if (upperStartPoint != null)
            return upperStartPoint.position;

        return transform.position;
    }

    private void SetCopyPose(Transform target, Vector3 position, Quaternion rotation)
    {
        CharacterController controller = target.GetComponent<CharacterController>();

        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            target.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
        }
        else
        {
            target.SetPositionAndRotation(position, rotation);
        }

        ResolvePenetration(target.gameObject);
        ClampToUpperBoundary(target.gameObject);
    }

    private void ResolvePenetration(GameObject copy)
    {
        Collider copyCollider = copy.GetComponent<Collider>();

        if (copyCollider == null)
            return;

        bool resolved = false;

        for (int iteration = 0; iteration < 4; iteration++)
        {
            if (!GetCapsule(copy, out Vector3 pointA, out Vector3 pointB, out float radius))
                break;

            Collider[] overlaps = Physics.OverlapCapsule(
                pointA,
                pointB,
                radius,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );

            bool moved = false;

            foreach (Collider other in overlaps)
            {
                if (other == null ||
                    other == copyCollider ||
                    other.transform.IsChildOf(copy.transform) ||
                    other.GetComponentInParent<TwoPointsCopy>() != null)
                {
                    continue;
                }

                if (!Physics.ComputePenetration(
                        copyCollider,
                        copy.transform.position,
                        copy.transform.rotation,
                        other,
                        other.transform.position,
                        other.transform.rotation,
                        out Vector3 direction,
                        out float distance
                    ))
                {
                    continue;
                }

                copy.transform.position += direction * (distance + collisionOffset);
                moved = true;
                resolved = true;
            }

            if (!moved)
                break;
        }

        if (resolved)
        {
            copyGravitySpeeds[copy] = 0f;
            stuckCopies.Remove(copy);
        }
    }

    private void ClampToUpperBoundary(GameObject copy)
    {
        Vector3 upperOrigin = GetUpperOrigin();
        float height = Vector3.Dot(copy.transform.position - upperOrigin, WorldUp);

        if (height <= 0f)
            return;

        copy.transform.position -= WorldUp * height;
        copyGravitySpeeds[copy] = 0f;
        stuckCopies.Remove(copy);
    }

    private void SyncHead(Transform source, Transform target)
    {
        if (source == null || target == null)
            return;

        Vector3 reflectedForward = Vector3.Reflect(source.forward, WorldUp);
        target.rotation = Quaternion.LookRotation(reflectedForward, -WorldUp);
    }

    private Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChild(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void OnDisable()
    {
        if (upperPlayer != null)
            Destroy(upperPlayer);

        upperPlayer = null;
        upperHead = null;
        upperPlayerInteractor = null;

        foreach (GameObject copy in cloneCopies.Values)
        {
            if (copy != null)
                Destroy(copy);
        }

        cloneCopies.Clear();
        stuckCopies.Clear();
        copyHeightOffsets.Clear();
        copyGravitySpeeds.Clear();
    }
}
