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

public class CloneRecordingData
{
    public readonly List<RecordedPose> Poses = new List<RecordedPose>();
    public readonly List<RecordedInteraction> Interactions = new List<RecordedInteraction>();
    public float Duration;
}
