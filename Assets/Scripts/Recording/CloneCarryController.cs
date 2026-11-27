using UnityEngine;

public class CloneCarryController : MonoBehaviour
{
    [SerializeField] private Transform carryPoint;
    [SerializeField] private ClonePlayback playback;
    [SerializeField, Min(0f)] private float pickupDistance = 2.5f;
    [SerializeField, Min(0f)] private float pickupDuration = 0.45f;
    [SerializeField, Min(0f)] private float dropDuration = 0.35f;
    [SerializeField, Min(0f)] private float followSpeed = 14f;
    [SerializeField, Min(0f)] private float movementSway = 0.02f;
    [SerializeField, Min(0f)] private float rotationStep = 15f;

    private CarryableItem heldItem;
    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private Quaternion holdRotation;
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
        {
            IsCarrying = false;
            state = CloneCarryState.None;
            return;
        }

        if (state == CloneCarryState.Lifting)
            UpdateLifting();
        else if (state == CloneCarryState.Holding)
        {
            UpdateRotation();
            UpdateHolding();
        }
        else if (state == CloneCarryState.Dropping)
            UpdateDropping();
    }

    public void Pickup(CarryableItem item)
    {
        ReleaseCurrentItem();

        if (item == null ||
            Vector3.Distance(transform.position, item.transform.position) >
            Mathf.Max(0f, pickupDistance))
        {
            return;
        }

        if (!item.TryTake(gameObject))
            return;

        heldItem = item;
        IsCarrying = true;
        swayTime = 0f;
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
        dropPosition = SaveDataValidator.Finite(position, heldItem.transform.position);
        dropRotation = SaveDataValidator.Rotation(rotation);
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

        if (ApplyRecordedPose())
        {
            if (progress >= 1f)
                state = CloneCarryState.Holding;

            return;
        }

        heldItem.transform.position = Vector3.Lerp(transitionStartPosition, GetHoldPosition(), progress);
        heldItem.transform.rotation = Quaternion.Slerp(transitionStartRotation, GetHoldRotation(), progress);

        if (progress >= 1f)
            state = CloneCarryState.Holding;
    }

    private void UpdateHolding()
    {
        if (ApplyRecordedPose())
            return;

        float follow = 1f - Mathf.Exp(
            -Mathf.Max(0f, SaveDataValidator.Finite(followSpeed, 14f)) *
            Time.deltaTime
        );
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
        return carryPoint.rotation * holdRotation;
    }

    private bool ApplyRecordedPose()
    {
        if (playback == null || !playback.HasCarryPose)
            return false;

        heldItem.transform.SetPositionAndRotation(
            transform.TransformPoint(playback.CarryLocalPosition),
            transform.rotation * playback.CarryLocalRotation
        );
        return true;
    }

    private void UpdateRotation()
    {
        if (playback == null || Mathf.Abs(playback.ScrollInputThisFrame) < 0.01f)
            return;

        float angle = Mathf.Sign(playback.ScrollInputThisFrame) *
                      Mathf.Max(0f, SaveDataValidator.Finite(rotationStep, 15f));
        holdRotation = Quaternion.AngleAxis(angle, Vector3.up) * holdRotation;
    }

    private float SmoothProgress(float time, float duration)
    {
        float progress = Mathf.Clamp01(
            SaveDataValidator.Finite(time, 0f) /
            Mathf.Clamp(SaveDataValidator.Finite(duration, 0.01f), 0.01f, 10f)
        );
        return progress * progress * (3f - 2f * progress);
    }

    private void ReleaseCurrentItem()
    {
        IsCarrying = false;

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
        IsCarrying = false;
        state = CloneCarryState.None;
        item.Release(gameObject, position, rotation);
    }

    private void OnDestroy()
    {
        StopCarrying();
    }
}
