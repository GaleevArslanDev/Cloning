using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarry : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Vector3 holdOffset = new Vector3(0.35f, -0.25f, 1.3f);
    [SerializeField] private float pickupDuration = 0.45f;
    [SerializeField] private float dropDuration = 0.35f;
    [SerializeField] private float followSpeed = 14f;
    [SerializeField] private float dropSearchDistance = 8f;
    [SerializeField] private LayerMask dropSurfaceLayers = ~0;
    [SerializeField] private float movementSway = 0.025f;

    private CarryableItem heldItem;
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private Vector3 dropPosition;
    private Quaternion dropRotation;
    private Quaternion restingRotation;
    private float transitionTime;
    private float swayTime;
    private bool canDrop;
    private CarryState state;

    public bool IsBusy => heldItem != null;

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
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (heldItem == null)
        {
            state = CarryState.None;
            return;
        }

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
        }
    }

    private void LateUpdate()
    {
        if (heldItem == null || playerCamera == null)
            return;

        if (state == CarryState.Lifting)
            UpdateLifting();
        else if (state == CarryState.Holding)
            UpdateHolding();
        else if (state == CarryState.Dropping)
            UpdateDropping();
    }

    public bool TryPickup(CarryableItem item)
    {
        if (heldItem != null || item == null || !item.TryTake(gameObject))
            return false;

        heldItem = item;
        restingRotation = item.transform.rotation;
        transitionStartPosition = item.transform.position;
        transitionStartRotation = item.transform.rotation;
        transitionTime = 0f;
        swayTime = 0f;
        canDrop = false;
        state = CarryState.Lifting;

        ItemPickedUp?.Invoke(item);
        return true;
    }

    private void UpdateLifting()
    {
        transitionTime += Time.deltaTime;
        float progress = Mathf.Clamp01(transitionTime / Mathf.Max(0.01f, pickupDuration));
        progress = progress * progress * (3f - 2f * progress);

        heldItem.transform.position = Vector3.Lerp(transitionStartPosition, GetHoldPosition(), progress);
        heldItem.transform.rotation = Quaternion.Slerp(transitionStartRotation, GetHoldRotation(), progress);

        if (progress >= 1f)
            state = CarryState.Holding;
    }

    private void UpdateHolding()
    {
        float follow = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        heldItem.transform.position = Vector3.Lerp(heldItem.transform.position, GetHoldPosition(), follow);
        heldItem.transform.rotation = Quaternion.Slerp(heldItem.transform.rotation, GetHoldRotation(), follow);
    }

    private void StartDrop()
    {
        transitionStartPosition = heldItem.transform.position;
        transitionStartRotation = heldItem.transform.rotation;
        FindDropPose(out dropPosition, out dropRotation);
        transitionTime = 0f;
        state = CarryState.Dropping;

        ItemDropped?.Invoke(heldItem, dropPosition, dropRotation);
    }

    private void UpdateDropping()
    {
        transitionTime += Time.deltaTime;
        float progress = Mathf.Clamp01(transitionTime / Mathf.Max(0.01f, dropDuration));
        progress = progress * progress * (3f - 2f * progress);

        heldItem.transform.position = Vector3.Lerp(transitionStartPosition, dropPosition, progress);
        heldItem.transform.rotation = Quaternion.Slerp(transitionStartRotation, dropRotation, progress);

        if (progress < 1f)
            return;

        CarryableItem item = heldItem;
        heldItem = null;
        state = CarryState.None;
        item.Release(gameObject, dropPosition, dropRotation);
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

        return playerCamera.transform.TransformPoint(holdOffset + heldItem.HoldPositionOffset + sway);
    }

    private Quaternion GetHoldRotation()
    {
        return playerCamera.transform.rotation * Quaternion.Euler(heldItem.HoldRotationOffset);
    }

    private void FindDropPose(out Vector3 position, out Quaternion rotation)
    {
        rotation = restingRotation;
        Vector3 origin = heldItem.transform.position + Vector3.up * 0.2f;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            dropSearchDistance,
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
            position = heldItem.transform.position;
            return;
        }

        Vector3 surfacePoint = nearestHit.point;
        Vector3 surfaceNormal = nearestHit.normal.normalized;
        MirrorPlacement mirrorPlacement = heldItem.GetComponent<MirrorPlacement>();

        if (mirrorPlacement != null)
        {
            rotation = mirrorPlacement.GetDropRotation(nearestHit, heldItem.transform.rotation);
            surfacePoint = mirrorPlacement.GetSurfacePoint(nearestHit);
            surfaceNormal = mirrorPlacement.GetSurfaceNormal(nearestHit);
        }

        float bottomOffset = GetBottomOffset(rotation, surfaceNormal);
        position = surfacePoint + surfaceNormal * (bottomOffset + 0.01f);
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
        if (heldItem != null)
            heldItem.Release(gameObject, heldItem.transform.position, heldItem.transform.rotation);
    }
}
