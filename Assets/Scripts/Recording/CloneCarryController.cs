using UnityEngine;

public class CloneCarryController : MonoBehaviour
{
    [SerializeField] private Transform carryPoint;
    [SerializeField] private ClonePlayback playback;
    [SerializeField] private float pickupDistance = 2.5f;
    [SerializeField] private float pickupDuration = 0.45f;
    [SerializeField] private float dropDuration = 0.35f;
    [SerializeField] private float followSpeed = 14f;
    [SerializeField] private float movementSway = 0.02f;

    private CarryableItem heldItem;
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private Vector3 dropPosition;
    private Quaternion dropRotation;
    private float transitionTime;
    private float swayTime;
    private CloneCarryState state;

    public bool IsCarrying { get; private set; }

    private enum CloneCarryState
    {
        None,
        Lifting,
        Holding,
        Dropping
    }

    private void Awake()
    {
        if (carryPoint == null)
            carryPoint = transform;
        if (playback == null)
            playback = GetComponent<ClonePlayback>();
    }

    private void LateUpdate()
    {
        if (heldItem == null)
            return;

        if (state == CloneCarryState.Lifting)
            UpdateLifting();
        else if (state == CloneCarryState.Holding)
            UpdateHolding();
        else if (state == CloneCarryState.Dropping)
            UpdateDropping();
    }

    public void Pickup(CarryableItem item)
    {
        ReleaseCurrentItem();
        IsCarrying = true;
        state = CloneCarryState.Holding;
        swayTime = 0f;

        if (item == null || Vector3.Distance(transform.position, item.transform.position) > pickupDistance)
            return;

        if (!item.TryTake(gameObject))
            return;

        heldItem = item;
        transitionStartPosition = item.transform.position;
        transitionStartRotation = item.transform.rotation;
        transitionTime = 0f;
        state = CloneCarryState.Lifting;
    }

    public void Drop(Vector3 position, Quaternion rotation)
    {
        IsCarrying = false;

        if (heldItem == null)
        {
            state = CloneCarryState.None;
            return;
        }

        transitionStartPosition = heldItem.transform.position;
        transitionStartRotation = heldItem.transform.rotation;
        dropPosition = position;
        dropRotation = rotation;
        transitionTime = 0f;
        state = CloneCarryState.Dropping;
    }

    public void StopCarrying()
    {
        IsCarrying = false;
        ReleaseCurrentItem();
    }

    private void UpdateLifting()
    {
        transitionTime += Time.deltaTime;
        float progress = SmoothProgress(transitionTime, pickupDuration);

        heldItem.transform.position = Vector3.Lerp(transitionStartPosition, GetHoldPosition(), progress);
        heldItem.transform.rotation = Quaternion.Slerp(transitionStartRotation, GetHoldRotation(), progress);

        if (progress >= 1f)
            state = CloneCarryState.Holding;
    }

    private void UpdateHolding()
    {
        float follow = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        heldItem.transform.position = Vector3.Lerp(heldItem.transform.position, GetHoldPosition(), follow);
        heldItem.transform.rotation = Quaternion.Slerp(heldItem.transform.rotation, GetHoldRotation(), follow);
    }

    private void UpdateDropping()
    {
        transitionTime += Time.deltaTime;
        float progress = SmoothProgress(transitionTime, dropDuration);

        heldItem.transform.position = Vector3.Lerp(transitionStartPosition, dropPosition, progress);
        heldItem.transform.rotation = Quaternion.Slerp(transitionStartRotation, dropRotation, progress);

        if (progress >= 1f)
            ReleaseCurrentItem(dropPosition, dropRotation);
    }

    private Vector3 GetHoldPosition()
    {
        Vector3 sway = Vector3.zero;

        if (playback != null && !playback.IsStandingStill)
        {
            swayTime += Time.deltaTime * 7f;
            sway.x = Mathf.Cos(swayTime * 0.5f) * movementSway;
            sway.y = Mathf.Sin(swayTime) * movementSway;
        }

        return carryPoint.TransformPoint(heldItem.HoldPositionOffset + sway);
    }

    private Quaternion GetHoldRotation()
    {
        return carryPoint.rotation * Quaternion.Euler(heldItem.HoldRotationOffset);
    }

    private float SmoothProgress(float time, float duration)
    {
        float progress = Mathf.Clamp01(time / Mathf.Max(0.01f, duration));
        return progress * progress * (3f - 2f * progress);
    }

    private void ReleaseCurrentItem()
    {
        if (heldItem != null)
            ReleaseCurrentItem(heldItem.transform.position, heldItem.transform.rotation);
        else
            state = CloneCarryState.None;
    }

    private void ReleaseCurrentItem(Vector3 position, Quaternion rotation)
    {
        if (heldItem == null)
            return;

        CarryableItem item = heldItem;
        heldItem = null;
        state = CloneCarryState.None;
        item.Release(gameObject, position, rotation);
    }

    private void OnDestroy()
    {
        StopCarrying();
    }
}
