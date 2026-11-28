using System;
using UnityEngine;

[Serializable]
public class BackgroundMusicTrack
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField, Min(0f)] private float transitionDuration = 2f;

    public AudioClip Clip => clip;
    public float Volume => IsFinite(volume) ? Mathf.Clamp01(volume) : 0.5f;
    public float TransitionDuration => IsFinite(transitionDuration)
        ? Mathf.Clamp(transitionDuration, 0f, 30f)
        : 2f;

    private bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
