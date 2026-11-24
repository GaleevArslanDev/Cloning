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
    [SerializeField] private float delayBefore;
    [SerializeField] private float duration = 3f;
    [SerializeField] private float delayAfter = 0.25f;

    public string Speaker => speaker;
    public string Text => text;
    public AudioClip Voice => voice;
    public float DelayBefore => Mathf.Max(0f, delayBefore);
    public float Duration => Mathf.Max(0.05f, duration);
    public float DelayAfter => Mathf.Max(0f, delayAfter);
}
