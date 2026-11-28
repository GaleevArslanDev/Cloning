using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarry : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Vector3 holdOffset = new Vector3(0.35f, -0.25f, 1.3f);
    [SerializeField, Min(0f)] private float pickupDuration = 0.45f;
    [SerializeField, Min(0f)] private float dropDuration = 0.35f;
    [SerializeField, Min(0f)] private float followSpeed = 14f;
    [SerializeField, Min(0f)] private float dropSearchDistance = 8f;
    [SerializeField] private LayerMask dropSurfaceLayers = ~0;
    [SerializeField] private LayerMask dropBlockingLayers = ~0;
    [SerializeField, Min(0f)] private float allowedDropOverlap = 0.002f;
    [SerializeField, Min(0f)] private float dropCorrectionDistance = 0.8f;
    [SerializeField, Min(0.001f)] private float dropCorrectionStep = 0.1f;
    [SerializeField, Min(0f)] private float movementSway = 0.025f;
    [SerializeField, Min(0f)] private float rotationStep = 15f;
    private AudioSource carryAudioSource;
    private AudioClip pickupSound;
    private AudioClip dropSound;

    private CarryableItem heldItem;
    private Transform holdTransform;
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private Vector3 dropPosition;
    private Quaternion dropRotation;
    private Quaternion holdRotation;
    private float transitionTime;
    private float swayTime;
    private bool canDrop;
    private bool useMouseInput = true;
    private Vector3 dropDirection = Vector3.down;
    private CarryState state;

    public bool IsBusy => heldItem != null;
    public CarryableItem HeldItem => heldItem;

    public event Action<CarryableItem> ItemPickedUp;
    public event Action<CarryableItem, Vector3, Quaternion> ItemDropped;

    private enum CarryState
    {
        None,
        Lifting,
        Holding,
        Dropping
    }

    private void Awake()
    {
        carryAudioSource = PlayerAudioController.GetOneShotSource();
        GameAudioClips audioClips = GameAudioClips.Instance;

        if (audioClips != null)
        {
            pickupSound = audioClips.Pickup;
            dropSound = audioClips.Drop;
        }

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerCamera != null)
            holdTransform = playerCamera.transform;
    }

    private void Update()
    {
        if (PauseMenuController.IsGamePaused) return;

        if (heldItem == null)
        {
            state = CarryState.None;
            return;
        }

        if (!useMouseInput)
            return;

        if (!canDrop)
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
                canDrop = true;

            return;
        }

        if (state == CarryState.Holding &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartDrop();
            return;
        }

        if (state == CarryState.Holding)
            UpdateRotationInput();
    }

    private void LateUpdate()
    {
        if (PauseMenuController.IsGamePaused) return;

        if (heldItem == null)
            return;

        if (holdTransform == null)
        {
            Debug.LogWarning("PlayerCarry: потеряна точка удержания, предмет отпущен", this);
            CancelCarry();
            return;
        }

        if (state == CarryState.Lifting)
            UpdateLifting();
        else if (state == CarryState.Holding)
            UpdateHolding();
        else if (state == CarryState.Dropping)
            UpdateDropping();
    }

    public bool TryPickup(CarryableItem item)
    {
        if (heldItem != null || item == null)
            return false;

        if (holdTransform == null)
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            holdTransform = playerCamera != null ? playerCamera.transform : null;

            if (holdTransform == null)
            {
                Debug.LogWarning("PlayerCarry: не назначена камера или точка удержания", this);
                return false;
            }
        }

        if (!item.TryTake(gameObject))
            return false;

        heldItem = item;
        Quaternion relativeRotation = Quaternion.Inverse(transform.rotation) * item.transform.rotation;
        Vector3 defaultRotation = item.HoldRotationOffset;
        holdRotation = Quaternion.Euler(
            defaultRotation.x,
            relativeRotation.eulerAngles.y,
            defaultRotation.z
        );
        transitionStartPosition = item.transform.position;
        transitionStartRotation = item.transform.rotation;
        transitionTime = 0f;
        swayTime = 0f;
        canDrop = !useMouseInput;
        state = CarryState.Lifting;

        PlaySound(pickupSound);
        ItemPickedUp?.Invoke(item);
        return true;
    }

    public void ConfigureExternalInput(Transform newHoldTransform, Vector3 newDropDirection)
    {
        if (newHoldTransform == null)
        {
            Debug.LogWarning("PlayerCarry: внешняя точка удержания не назначена", this);
            return;
        }

        holdTransform = newHoldTransform;
        playerMovement = null;
        useMouseInput = false;
        dropDirection = newDropDirection.sqrMagnitude > 0.0001f
            ? newDropDirection.normalized
            : Vector3.down;
    }

    public void RequestDrop()
    {
        if (heldItem != null && state == CarryState.Holding)
            StartDrop();
    }

    public void RequestRotation(float scroll)
    {
        if (heldItem == null || state != CarryState.Holding || Mathf.Abs(scroll) < 0.01f)
            return;

        float angle = Mathf.Sign(scroll) *
                      Mathf.Max(0f, SaveDataValidator.Finite(rotationStep, 15f));
        holdRotation = Quaternion.AngleAxis(angle, Vector3.up) * holdRotation;
    }

    public bool TryGetHeldPose(
        Transform relativeTo,
        out Vector3 localPosition,
        out Quaternion localRotation
    )
    {
        if (heldItem == null || relativeTo == null)
        {
            localPosition = Vector3.zero;
            localRotation = Quaternion.identity;
            return false;
        }

        localPosition = relativeTo.InverseTransformPoint(heldItem.transform.position);
        localRotation = Quaternion.Inverse(relativeTo.rotation) * heldItem.transform.rotation;
        return true;
    }

    public bool RestoreHeldItem(CarryableItem item)
    {
        CancelCarry();

        if (item == null || holdTransform == null || !item.TryTake(gameObject))
            return false;

        heldItem = item;
        holdRotation = Quaternion.Inverse(holdTransform.rotation) * item.transform.rotation;
        swayTime = 0f;
        transitionTime = 0f;
        canDrop = true;
        state = CarryState.Holding;
        return true;
    }

    private void UpdateLifting()
    {
        transitionTime += Time.deltaTime;
        float progress = Mathf.Clamp01(
            transitionTime / Mathf.Clamp(
                SaveDataValidator.Finite(pickupDuration, 0.45f),
                0.01f,
                10f
            )
        );
        progress = progress * progress * (3f - 2f * progress);

        heldItem.transform.position = Vector3.Lerp(transitionStartPosition, GetHoldPosition(), progress);
        heldItem.transform.rotation = Quaternion.Slerp(transitionStartRotation, GetHoldRotation(), progress);

        if (progress >= 1f)
            state = CarryState.Holding;
    }

    private void UpdateHolding()
    {
        float follow = 1f - Mathf.Exp(
            -Mathf.Max(0f, SaveDataValidator.Finite(followSpeed, 14f)) *
            Time.deltaTime
        );
        heldItem.transform.position = Vector3.Lerp(heldItem.transform.position, GetHoldPosition(), follow);
        heldItem.transform.rotation = Quaternion.Slerp(heldItem.transform.rotation, GetHoldRotation(), follow);
    }

    private void StartDrop()
    {
        if (!TryFindDropPose(out dropPosition, out dropRotation))
            return;

        transitionStartPosition = heldItem.transform.position;
        transitionStartRotation = heldItem.transform.rotation;
        transitionTime = 0f;
        state = CarryState.Dropping;

        ItemDropped?.Invoke(heldItem, dropPosition, dropRotation);
    }

    private void UpdateDropping()
    {
        transitionTime += Time.deltaTime;
        float progress = Mathf.Clamp01(
            transitionTime / Mathf.Clamp(
                SaveDataValidator.Finite(dropDuration, 0.35f),
                0.01f,
                10f
            )
        );
        progress = progress * progress * (3f - 2f * progress);

        heldItem.transform.position = Vector3.Lerp(transitionStartPosition, dropPosition, progress);
        heldItem.transform.rotation = Quaternion.Slerp(transitionStartRotation, dropRotation, progress);

        if (progress < 1f)
            return;

        CarryableItem item = heldItem;
        heldItem = null;
        state = CarryState.None;
        item.Release(gameObject, dropPosition, dropRotation);
        PlaySound(dropSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (carryAudioSource != null && clip != null)
            carryAudioSource.PlayOneShot(clip);
    }

    private Vector3 GetHoldPosition()
    {
        Vector3 sway = Vector3.zero;

        if (playerMovement != null && playerMovement.IsMoving)
        {
            swayTime += Time.deltaTime * (playerMovement.IsRunning ? 10f : 7f);
            sway.x = Mathf.Cos(swayTime * 0.5f) * movementSway;
            sway.y = Mathf.Sin(swayTime) * movementSway;
        }
        else
        {
            swayTime += Time.deltaTime * 1.5f;
            sway.y = Mathf.Sin(swayTime) * movementSway * 0.25f;
        }

        return holdTransform.TransformPoint(holdOffset + heldItem.HoldPositionOffset + sway);
    }

    private Quaternion GetHoldRotation()
    {
        return holdTransform.rotation * holdRotation;
    }

    private void UpdateRotationInput()
    {
        if (Mouse.current == null)
            return;

        RequestRotation(Mouse.current.scroll.ReadValue().y);
    }

    private bool TryFindDropPose(out Vector3 position, out Quaternion rotation)
    {
        Vector3 itemPosition = heldItem.transform.position;

        if (!TryGetDropPoseAt(
                itemPosition,
                out position,
                out rotation,
                out Vector3 surfaceNormal,
                out Collider surfaceCollider
            ))
            return false;

        if (IsDropPoseClear(position, rotation) &&
            IsDropPathClear(position, surfaceCollider))
            return true;

        Vector3 towardPlayer = Vector3.ProjectOnPlane(
            transform.position - position,
            surfaceNormal
        );

        if (towardPlayer.sqrMagnitude < 0.001f)
        {
            Vector3 fallbackDirection = holdTransform != null
                ? -holdTransform.forward
                : -transform.forward;
            towardPlayer = Vector3.ProjectOnPlane(fallbackDirection, surfaceNormal);
        }

        if (towardPlayer.sqrMagnitude < 0.001f)
            towardPlayer = Vector3.Cross(surfaceNormal, Vector3.right);

        towardPlayer.Normalize();
        float step = Mathf.Max(
            0.02f,
            SaveDataValidator.Finite(dropCorrectionStep, 0.1f)
        );
        float maximumDistance = Mathf.Max(
            0f,
            SaveDataValidator.Finite(dropCorrectionDistance, 0f)
        );
        float[] angles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f };

        for (float distance = step; distance <= maximumDistance + 0.001f; distance += step)
        {
            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, surfaceNormal) * towardPlayer;
                Vector3 searchPosition = itemPosition + direction * distance;

                if (!TryGetDropPoseAt(
                        searchPosition,
                        out Vector3 correctedPosition,
                        out Quaternion correctedRotation,
                        out _,
                        out Collider correctedSurface
                    ))
                {
                    continue;
                }

                if (!IsDropPoseClear(correctedPosition, correctedRotation) ||
                    !IsDropPathClear(correctedPosition, correctedSurface))
                    continue;

                position = correctedPosition;
                rotation = correctedRotation;
                return true;
            }
        }

        return false;
    }

    private bool TryGetDropPoseAt(
        Vector3 itemPosition,
        out Vector3 position,
        out Quaternion rotation,
        out Vector3 surfaceNormal,
        out Collider surfaceCollider
    )
    {
        rotation = heldItem.transform.rotation;
        surfaceNormal = -dropDirection;
        surfaceCollider = null;
        Vector3 origin = itemPosition - dropDirection * 0.2f;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            dropDirection,
            Mathf.Max(0f, SaveDataValidator.Finite(dropSearchDistance, 0f)),
            dropSurfaceLayers,
            QueryTriggerInteraction.Ignore
        );

        float nearestDistance = float.MaxValue;
        RaycastHit nearestHit = new RaycastHit();
        bool surfaceFound = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(heldItem.transform) ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
                surfaceFound = true;
            }
        }

        if (!surfaceFound)
        {
            position = itemPosition;
            return false;
        }

        Vector3 surfacePoint = nearestHit.point;
        surfaceNormal = nearestHit.normal.normalized;
        surfaceCollider = nearestHit.collider;
        MirrorPlacement mirrorPlacement = heldItem.GetComponent<MirrorPlacement>();

        if (mirrorPlacement != null)
        {
            rotation = mirrorPlacement.GetDropRotation(nearestHit, heldItem.transform.rotation);
            surfacePoint = mirrorPlacement.GetSurfacePoint(nearestHit);
            surfaceNormal = mirrorPlacement.GetSurfaceNormal(nearestHit);
        }

        float bottomOffset = GetBottomOffset(rotation, surfaceNormal);
        position = surfacePoint + surfaceNormal * (bottomOffset + 0.01f);
        return true;
    }

    private bool IsDropPathClear(Vector3 targetPosition, Collider surfaceCollider)
    {
        Vector3 origin = holdTransform != null
            ? holdTransform.position
            : transform.position;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction / distance,
            distance,
            dropBlockingLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null ||
                hit.collider == surfaceCollider ||
                hit.collider.transform.IsChildOf(heldItem.transform) ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < distance - 0.05f)
                return false;
        }

        return true;
    }

    private bool IsDropPoseClear(Vector3 position, Quaternion rotation)
    {
        Vector3 currentPosition = heldItem.transform.position;
        Quaternion currentRotation = heldItem.transform.rotation;
        heldItem.transform.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();

        Collider[] itemColliders = heldItem.GetComponentsInChildren<Collider>();
        bool isClear = true;

        foreach (Collider itemCollider in itemColliders)
        {
            if (itemCollider == null || !itemCollider.enabled)
                continue;

            Bounds bounds = itemCollider.bounds;
            Collider[] overlaps = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                Quaternion.identity,
                dropBlockingLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider other in overlaps)
            {
                if (other == null ||
                    other.transform.IsChildOf(heldItem.transform) ||
                    other.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!Physics.ComputePenetration(
                        itemCollider,
                        itemCollider.transform.position,
                        itemCollider.transform.rotation,
                        other,
                        other.transform.position,
                        other.transform.rotation,
                        out Vector3 direction,
                        out float distance
                    ))
                {
                    continue;
                }

                if (distance > Mathf.Max(
                        0f,
                        SaveDataValidator.Finite(allowedDropOverlap, 0f)
                    ))
                {
                    isClear = false;
                    break;
                }
            }

            if (!isClear)
                break;
        }

        heldItem.transform.SetPositionAndRotation(currentPosition, currentRotation);
        Physics.SyncTransforms();
        return isClear;
    }

    private float GetBottomOffset(Quaternion targetRotation, Vector3 surfaceNormal)
    {
        Quaternion currentRotation = heldItem.transform.rotation;
        heldItem.transform.rotation = targetRotation;
        Physics.SyncTransforms();

        Bounds bounds = heldItem.GetBounds();
        Vector3 centerOffset = bounds.center - heldItem.transform.position;
        Vector3 extents = bounds.extents;
        Vector3 normal = surfaceNormal.normalized;

        float radius = Mathf.Abs(normal.x) * extents.x +
                       Mathf.Abs(normal.y) * extents.y +
                       Mathf.Abs(normal.z) * extents.z;

        float bottomOffset = radius - Vector3.Dot(centerOffset, normal);

        heldItem.transform.rotation = currentRotation;
        Physics.SyncTransforms();
        return Mathf.Max(0.02f, bottomOffset);
    }

    private void OnDestroy()
    {
        CancelCarry();
    }

    public void CancelCarry()
    {
        if (heldItem == null)
            return;

        CarryableItem item = heldItem;
        heldItem = null;
        state = CarryState.None;
        canDrop = false;
        item.Release(gameObject, item.transform.position, item.transform.rotation);
    }
}
