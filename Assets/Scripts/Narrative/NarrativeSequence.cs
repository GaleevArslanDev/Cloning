using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Narrative Sequence", menuName = "Narrative/Sequence")]
public class NarrativeSequence : ScriptableObject
{
    [SerializeField] private NarrativeLine[] lines;

    public NarrativeLine[] Lines => lines;
}

[Serializable]
public class NarrativeLine
{
    [SerializeField] private string speaker;
    [SerializeField, TextArea(2, 6)] private string text;
    [SerializeField] private AudioClip voice;
    [SerializeField, Min(0f)] private float delayBefore;
    [SerializeField, Min(0f)] private float duration = 3f;
    [SerializeField, Min(0f)] private float delayAfter = 0.25f;

    public string Speaker => speaker ?? string.Empty;
    public string Text => text ?? string.Empty;
    public AudioClip Voice => voice;
    public float DelayBefore => Mathf.Max(
        0f,
        SaveDataValidator.Finite(delayBefore, 0f)
    );
    public float Duration => Mathf.Max(
        0.05f,
        SaveDataValidator.Finite(duration, 0.05f)
    );
    public float DelayAfter => Mathf.Max(
        0f,
        SaveDataValidator.Finite(delayAfter, 0f)
    );
}
