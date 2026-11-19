using System.Collections.Generic;
using UnityEngine;

public class ClonePlayback : MonoBehaviour
{
    private static readonly HashSet<ClonePlayback> activeClones = new HashSet<ClonePlayback>();

    [SerializeField] private Transform head;
    [SerializeField] private float standingSpeed = 0.05f;
    [SerializeField] private CloneCarryController carryController;

    private readonly HashSet<MonoBehaviour> activeInteractions = new HashSet<MonoBehaviour>();
    private CloneRecordingData recording;
    private int poseIndex;
    private int interactionIndex;
    private int carryActionIndex;
    private int inputActionIndex;
    private float playbackTime;
    private bool isFinished;

    public bool IsStandingStill { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsRidingClone { get; private set; }
    public bool IsLeftInputHeld { get; private set; }
    public bool RightInputPressedThisFrame { get; private set; }
    public float ScrollInputThisFrame { get; private set; }

    private void OnEnable()
    {
        activeClones.Add(this);
    }

    private void OnDisable()
    {
        activeClones.Remove(this);
    }

    public bool HasCloneBelow(
        float horizontalDistance,
        float minimumVerticalDistance,
        float maximumVerticalDistance
    )
    {
        Vector3 up = Physics.gravity.sqrMagnitude > 0.001f
            ? -Physics.gravity.normalized
            : Vector3.up;

        foreach (ClonePlayback clone in activeClones)
        {
            if (clone == null || clone == this || !clone.gameObject.activeInHierarchy)
                continue;

            Vector3 difference = transform.position - clone.transform.position;
            float verticalDistance = Vector3.Dot(difference, up);

            if (verticalDistance < minimumVerticalDistance ||
                verticalDistance > maximumVerticalDistance)
            {
                continue;
            }

            Vector3 horizontalOffset = Vector3.ProjectOnPlane(difference, up);

            if (horizontalOffset.magnitude <= horizontalDistance)
                return true;
        }

        return false;
    }

    public void Initialize(CloneRecordingData recordingData)
    {
        if (GetComponent<CharacterAnimationController>() == null)
            gameObject.AddComponent<CharacterAnimationController>();

        if (carryController == null)
            carryController = GetComponent<CloneCarryController>();

        recording = recordingData;
        poseIndex = 0;
        interactionIndex = 0;
        carryActionIndex = 0;
        inputActionIndex = 0;
        playbackTime = 0f;
        isFinished = false;
        IsLeftInputHeld = false;
        IsJumping = false;
        IsRidingClone = false;
        RightInputPressedThisFrame = false;
        ScrollInputThisFrame = 0f;

        if (recording.Poses.Count > 0)
            ApplyPose(recording.Poses[0]);
    }

    private void Update()
    {
        if (isFinished || recording == null || recording.Poses.Count == 0)
            return;

        playbackTime += Time.deltaTime;
        RightInputPressedThisFrame = false;
        ScrollInputThisFrame = 0f;

        UpdatePose();
        UpdateInteractions();
        UpdateCarryActions();
        UpdateInputActions();

        if (playbackTime >= recording.Duration)
            StopPlayback();
    }

    private void UpdatePose()
    {
        while (poseIndex < recording.Poses.Count - 2 &&
               recording.Poses[poseIndex + 1].Time <= playbackTime)
        {
            poseIndex++;
        }

        RecordedPose current = recording.Poses[poseIndex];
        RecordedPose next = recording.Poses[Mathf.Min(poseIndex + 1, recording.Poses.Count - 1)];
        float duration = next.Time - current.Time;
        float progress = duration > 0f ? (playbackTime - current.Time) / duration : 1f;

        transform.position = Vector3.Lerp(current.Position, next.Position, progress);
        transform.rotation = Quaternion.Slerp(current.BodyRotation, next.BodyRotation, progress);

        if (head != null)
            head.localRotation = Quaternion.Slerp(current.HeadRotation, next.HeadRotation, progress);

        float speed = duration > 0f
            ? Vector3.Distance(current.Position, next.Position) / duration
            : 0f;

        IsStandingStill = speed <= standingSpeed;
        IsJumping = current.IsJumping || next.IsJumping;
        IsRidingClone = current.IsRidingClone && next.IsRidingClone;
    }

    private void ApplyPose(RecordedPose pose)
    {
        transform.SetPositionAndRotation(pose.Position, pose.BodyRotation);

        if (head != null)
            head.localRotation = pose.HeadRotation;

        IsJumping = pose.IsJumping;
        IsRidingClone = pose.IsRidingClone;
    }

    private void UpdateInteractions()
    {
        while (interactionIndex < recording.Interactions.Count &&
               recording.Interactions[interactionIndex].Time <= playbackTime)
        {
            RecordedInteraction interaction = recording.Interactions[interactionIndex];
            PlayInteraction(interaction);
            interactionIndex++;
        }

        foreach (MonoBehaviour target in activeInteractions)
        {
            if (target != null && target is IInteractable interactable)
                interactable.Interact(gameObject, Time.deltaTime);
        }
    }

    private void PlayInteraction(RecordedInteraction interaction)
    {
        if (interaction.Target == null || !(interaction.Target is IInteractable interactable))
            return;

        if (interaction.Type == RecordedInteractionType.Begin)
        {
            if (activeInteractions.Add(interaction.Target))
                interactable.BeginInteraction(gameObject);
        }
        else if (activeInteractions.Remove(interaction.Target))
        {
            interactable.EndInteraction(gameObject);
        }
    }

    private void UpdateCarryActions()
    {
        while (carryActionIndex < recording.CarryActions.Count &&
               recording.CarryActions[carryActionIndex].Time <= playbackTime)
        {
            RecordedCarryAction action = recording.CarryActions[carryActionIndex];

            if (carryController != null)
            {
                if (action.Type == RecordedCarryActionType.Pickup)
                    carryController.Pickup(action.Item);
                else
                    carryController.Drop(action.DropPosition, action.DropRotation);
            }

            carryActionIndex++;
        }
    }

    private void UpdateInputActions()
    {
        while (inputActionIndex < recording.InputActions.Count &&
               recording.InputActions[inputActionIndex].Time <= playbackTime)
        {
            RecordedInputAction action = recording.InputActions[inputActionIndex];

            if (action.Type == RecordedInputType.LeftPressed)
                IsLeftInputHeld = true;
            else if (action.Type == RecordedInputType.LeftReleased)
                IsLeftInputHeld = false;
            else if (action.Type == RecordedInputType.RightPressed)
                RightInputPressedThisFrame = true;
            else if (action.Type == RecordedInputType.ScrollUp)
                ScrollInputThisFrame += 1f;
            else
                ScrollInputThisFrame -= 1f;

            inputActionIndex++;
        }
    }

    public void StopPlayback()
    {
        if (isFinished)
            return;

        isFinished = true;

        Cleanup();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void Cleanup()
    {
        foreach (MonoBehaviour target in activeInteractions)
        {
            if (target != null && target is IInteractable interactable)
                interactable.EndInteraction(gameObject);
        }

        activeInteractions.Clear();
        IsLeftInputHeld = false;
        IsJumping = false;
        IsRidingClone = false;
        RightInputPressedThisFrame = false;
        ScrollInputThisFrame = 0f;

        if (carryController != null)
            carryController.StopCarrying();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
