using System.Collections.Generic;
using UnityEngine;

public enum RecordedInteractionType
{
    Begin,
    End
}

public struct RecordedPose
{
    public float Time;
    public Vector3 Position;
    public Quaternion BodyRotation;
    public Quaternion HeadRotation;
}

public struct RecordedInteraction
{
    public float Time;
    public MonoBehaviour Target;
    public RecordedInteractionType Type;
}

public enum RecordedCarryActionType
{
    Pickup,
    Drop
}

public struct RecordedCarryAction
{
    public float Time;
    public CarryableItem Item;
    public RecordedCarryActionType Type;
    public Vector3 DropPosition;
    public Quaternion DropRotation;
}

public class CloneRecordingData
{
    public readonly List<RecordedPose> Poses = new List<RecordedPose>();
    public readonly List<RecordedInteraction> Interactions = new List<RecordedInteraction>();
    public readonly List<RecordedCarryAction> CarryActions = new List<RecordedCarryAction>();
    public float Duration;
}
