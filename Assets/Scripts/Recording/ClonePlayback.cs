using System.Collections.Generic;
using UnityEngine;

public class ClonePlayback : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private float standingSpeed = 0.05f;
    [SerializeField] private CloneCarryController carryController;

    private readonly HashSet<MonoBehaviour> activeInteractions = new HashSet<MonoBehaviour>();
    private CloneRecordingData recording;
    private int poseIndex;
    private int interactionIndex;
    private int carryActionIndex;
    private float playbackTime;

    public bool IsStandingStill { get; private set; }

    public void Initialize(CloneRecordingData recordingData)
    {
        if (carryController == null)
            carryController = GetComponent<CloneCarryController>();

        recording = recordingData;
        poseIndex = 0;
        interactionIndex = 0;
        carryActionIndex = 0;
        playbackTime = 0f;

        if (recording.Poses.Count > 0)
            ApplyPose(recording.Poses[0]);
    }

    private void Update()
    {
        if (recording == null || recording.Poses.Count == 0)
            return;

        playbackTime += Time.deltaTime;

        UpdatePose();
        UpdateInteractions();
        UpdateCarryActions();

        if (playbackTime >= recording.Duration)
            FinishPlayback();
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
    }

    private void ApplyPose(RecordedPose pose)
    {
        transform.SetPositionAndRotation(pose.Position, pose.BodyRotation);

        if (head != null)
            head.localRotation = pose.HeadRotation;
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

    private void FinishPlayback()
    {
        foreach (MonoBehaviour target in activeInteractions)
        {
            if (target != null && target is IInteractable interactable)
                interactable.EndInteraction(gameObject);
        }

        activeInteractions.Clear();

        if (carryController != null)
            carryController.StopCarrying();

        Destroy(gameObject);
    }
}
