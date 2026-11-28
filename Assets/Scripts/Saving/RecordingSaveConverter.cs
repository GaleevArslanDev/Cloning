using System;
using System.Collections.Generic;
using UnityEngine;

public static class RecordingSaveConverter
{
    public static RecordingSaveData ToSaveData(CloneRecordingData recording)
    {
        if (recording == null)
            return null;

        RecordingSaveData save = new RecordingSaveData
        {
            Duration = Mathf.Max(
                0f,
                SaveDataValidator.Finite(recording.Duration, 0f)
            )
        };

        foreach (RecordedPose pose in recording.Poses)
        {
            save.Poses.Add(new PoseSaveData
            {
                Time = Mathf.Max(0f, SaveDataValidator.Finite(pose.Time, 0f)),
                Position = SaveDataValidator.Finite(pose.Position, Vector3.zero),
                BodyRotation = SaveDataValidator.Rotation(pose.BodyRotation),
                HeadRotation = SaveDataValidator.Rotation(pose.HeadRotation),
                IsJumping = pose.IsJumping,
                IsRidingClone = pose.IsRidingClone,
                HasCarryPose = pose.HasCarryPose,
                CarryLocalPosition = pose.CarryLocalPosition,
                CarryLocalRotation = pose.CarryLocalRotation
            });
        }

        foreach (RecordedInteraction interaction in recording.Interactions)
        {
            if (interaction.Target == null)
                continue;

            save.Interactions.Add(new InteractionSaveData
            {
                Time = Mathf.Max(
                    0f,
                    SaveDataValidator.Finite(interaction.Time, 0f)
                ),
                TargetPath = SceneSaveReference.GetPath(interaction.Target),
                ComponentType = interaction.Target.GetType().FullName,
                ComponentIndex = SceneSaveReference.GetComponentIndex(interaction.Target),
                Type = interaction.Type
            });
        }

        foreach (RecordedCarryAction action in recording.CarryActions)
        {
            save.CarryActions.Add(new CarryActionSaveData
            {
                Time = Mathf.Max(0f, SaveDataValidator.Finite(action.Time, 0f)),
                ItemPath = SceneSaveReference.GetPath(action.Item),
                Type = action.Type,
                DropPosition = SaveDataValidator.Finite(action.DropPosition, Vector3.zero),
                DropRotation = SaveDataValidator.Rotation(action.DropRotation)
            });
        }

        foreach (RecordedInputAction action in recording.InputActions)
        {
            save.InputActions.Add(new InputActionSaveData
            {
                Time = Mathf.Max(0f, SaveDataValidator.Finite(action.Time, 0f)),
                Type = action.Type
            });
        }

        return save;
    }

    public static CloneRecordingData FromSaveData(RecordingSaveData save)
    {
        if (save == null)
            return null;

        CloneRecordingData recording = new CloneRecordingData
        {
            Duration = Mathf.Max(0f, SaveDataValidator.Finite(save.Duration, 0f))
        };

        foreach (PoseSaveData pose in save.Poses ?? new List<PoseSaveData>())
        {
            if (pose == null)
                continue;

            recording.Poses.Add(new RecordedPose
            {
                Time = Mathf.Max(0f, SaveDataValidator.Finite(pose.Time, 0f)),
                Position = SaveDataValidator.Finite(pose.Position, Vector3.zero),
                BodyRotation = SaveDataValidator.Rotation(pose.BodyRotation),
                HeadRotation = SaveDataValidator.Rotation(pose.HeadRotation),
                IsJumping = pose.IsJumping,
                IsRidingClone = pose.IsRidingClone,
                HasCarryPose = pose.HasCarryPose,
                CarryLocalPosition = SaveDataValidator.Finite(
                    pose.CarryLocalPosition,
                    Vector3.zero
                ),
                CarryLocalRotation = SaveDataValidator.Rotation(pose.CarryLocalRotation)
            });
        }

        foreach (InteractionSaveData interaction in
                 save.Interactions ?? new List<InteractionSaveData>())
        {
            if (interaction == null ||
                !Enum.IsDefined(typeof(RecordedInteractionType), interaction.Type))
            {
                continue;
            }

            MonoBehaviour target = SceneSaveReference.FindBehaviour(
                interaction.TargetPath,
                interaction.ComponentType,
                interaction.ComponentIndex
            );

            if (target == null)
                continue;

            recording.Interactions.Add(new RecordedInteraction
            {
                Time = Mathf.Max(0f, SaveDataValidator.Finite(interaction.Time, 0f)),
                Target = target,
                Type = interaction.Type
            });
        }

        foreach (CarryActionSaveData action in
                 save.CarryActions ?? new List<CarryActionSaveData>())
        {
            if (action == null ||
                !Enum.IsDefined(typeof(RecordedCarryActionType), action.Type))
            {
                continue;
            }

            CarryableItem item = SceneSaveReference.FindComponent<CarryableItem>(
                action.ItemPath
            );

            recording.CarryActions.Add(new RecordedCarryAction
            {
                Time = Mathf.Max(0f, SaveDataValidator.Finite(action.Time, 0f)),
                Item = item,
                Type = action.Type,
                DropPosition = SaveDataValidator.Finite(action.DropPosition, Vector3.zero),
                DropRotation = SaveDataValidator.Rotation(action.DropRotation)
            });
        }

        foreach (InputActionSaveData action in
                 save.InputActions ?? new List<InputActionSaveData>())
        {
            if (action == null ||
                !Enum.IsDefined(typeof(RecordedInputType), action.Type))
            {
                continue;
            }

            recording.InputActions.Add(new RecordedInputAction
            {
                Time = Mathf.Max(0f, SaveDataValidator.Finite(action.Time, 0f)),
                Type = action.Type
            });
        }

        recording.Poses.Sort((left, right) => left.Time.CompareTo(right.Time));
        recording.Interactions.Sort((left, right) => left.Time.CompareTo(right.Time));
        recording.CarryActions.Sort((left, right) => left.Time.CompareTo(right.Time));
        recording.InputActions.Sort((left, right) => left.Time.CompareTo(right.Time));

        if (recording.Poses.Count > 0)
        {
            recording.Duration = Mathf.Max(
                recording.Duration,
                recording.Poses[recording.Poses.Count - 1].Time
            );
        }

        if (recording.Interactions.Count > 0)
            recording.Duration = Mathf.Max(
                recording.Duration,
                recording.Interactions[recording.Interactions.Count - 1].Time
            );
        if (recording.CarryActions.Count > 0)
            recording.Duration = Mathf.Max(
                recording.Duration,
                recording.CarryActions[recording.CarryActions.Count - 1].Time
            );
        if (recording.InputActions.Count > 0)
            recording.Duration = Mathf.Max(
                recording.Duration,
                recording.InputActions[recording.InputActions.Count - 1].Time
            );

        return recording;
    }
}
