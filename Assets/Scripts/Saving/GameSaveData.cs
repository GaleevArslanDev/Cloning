using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int Version = 1;
    public string SlotName;
    public string SavedAt;
    public string SceneName;
    public bool LevelConditionCompleted;
    public PlayerSaveData Player = new PlayerSaveData();
    public CloneRecorderSaveData Recorder = new CloneRecorderSaveData();
    public List<CarryableSaveData> Carryables = new List<CarryableSaveData>();
    public List<ComponentSaveData> Components = new List<ComponentSaveData>();
}

[Serializable]
public class ComponentSaveData
{
    public string Path;
    public string ComponentType;
    public int ComponentIndex;
    public string Json;
}

[Serializable]
public class PlayerSaveData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float CameraPitch;
    public string HeldItemPath;
}

[Serializable]
public class CarryableSaveData
{
    public string Path;
    public Vector3 Position;
    public Quaternion Rotation;
    public bool Active;
    public bool IsKinematic;
    public bool UseGravity;
}

[Serializable]
public class CloneRecorderSaveData
{
    public float RecordingTime;
    public bool IsRecording;
    public RecordingSaveData CurrentRecording;
    public List<RecordingSaveData> SavedRecordings = new List<RecordingSaveData>();
}

[Serializable]
public class RecordingSaveData
{
    public float Duration;
    public List<PoseSaveData> Poses = new List<PoseSaveData>();
    public List<InteractionSaveData> Interactions = new List<InteractionSaveData>();
    public List<CarryActionSaveData> CarryActions = new List<CarryActionSaveData>();
    public List<InputActionSaveData> InputActions = new List<InputActionSaveData>();
}

[Serializable]
public class PoseSaveData
{
    public float Time;
    public Vector3 Position;
    public Quaternion BodyRotation;
    public Quaternion HeadRotation;
    public bool IsJumping;
    public bool IsRidingClone;
    public bool HasCarryPose;
    public Vector3 CarryLocalPosition;
    public Quaternion CarryLocalRotation;
}

[Serializable]
public class InteractionSaveData
{
    public float Time;
    public string TargetPath;
    public string ComponentType;
    public int ComponentIndex;
    public RecordedInteractionType Type;
}

[Serializable]
public class CarryActionSaveData
{
    public float Time;
    public string ItemPath;
    public RecordedCarryActionType Type;
    public Vector3 DropPosition;
    public Quaternion DropRotation;
}

[Serializable]
public class InputActionSaveData
{
    public float Time;
    public RecordedInputType Type;
}

public struct SaveSlotInfo
{
    public string Name;
    public string SceneName;
    public string SavedAt;
}
