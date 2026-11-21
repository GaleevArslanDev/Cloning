using UnityEngine;

public static class RecordingSaveConverter
{
    public static RecordingSaveData ToSaveData(CloneRecordingData recording)
    {
        if (recording == null)
            return null;

        RecordingSaveData save = new RecordingSaveData
        {
            Duration = recording.Duration
        };

        foreach (RecordedPose pose in recording.Poses)
        {
            save.Poses.Add(new PoseSaveData
            {
                Time = pose.Time,
                Position = pose.Position,
                BodyRotation = pose.BodyRotation,
                HeadRotation = pose.HeadRotation,
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
                Time = interaction.Time,
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
                Time = action.Time,
                ItemPath = SceneSaveReference.GetPath(action.Item),
                Type = action.Type,
                DropPosition = action.DropPosition,
                DropRotation = action.DropRotation
            });
        }

        foreach (RecordedInputAction action in recording.InputActions)
        {
            save.InputActions.Add(new InputActionSaveData
            {
                Time = action.Time,
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
            Duration = save.Duration
        };

        foreach (PoseSaveData pose in save.Poses)
        {
            recording.Poses.Add(new RecordedPose
            {
                Time = pose.Time,
                Position = pose.Position,
                BodyRotation = pose.BodyRotation,
                HeadRotation = pose.HeadRotation,
                IsJumping = pose.IsJumping,
                IsRidingClone = pose.IsRidingClone,
                HasCarryPose = pose.HasCarryPose,
                CarryLocalPosition = pose.CarryLocalPosition,
                CarryLocalRotation = pose.CarryLocalRotation
            });
        }

        foreach (InteractionSaveData interaction in save.Interactions)
        {
            MonoBehaviour target = SceneSaveReference.FindBehaviour(
                interaction.TargetPath,
                interaction.ComponentType,
                interaction.ComponentIndex
            );

            if (target == null)
                continue;

            recording.Interactions.Add(new RecordedInteraction
            {
                Time = interaction.Time,
                Target = target,
                Type = interaction.Type
            });
        }

        foreach (CarryActionSaveData action in save.CarryActions)
        {
            CarryableItem item = SceneSaveReference.FindComponent<CarryableItem>(
                action.ItemPath
            );

            recording.CarryActions.Add(new RecordedCarryAction
            {
                Time = action.Time,
                Item = item,
                Type = action.Type,
                DropPosition = action.DropPosition,
                DropRotation = action.DropRotation
            });
        }

        foreach (InputActionSaveData action in save.InputActions)
        {
            recording.InputActions.Add(new RecordedInputAction
            {
                Time = action.Time,
                Type = action.Type
            });
        }

        return recording;
    }
}
