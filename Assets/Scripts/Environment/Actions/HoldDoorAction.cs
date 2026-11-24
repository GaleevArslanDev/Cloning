using UnityEngine;

// Scrubs a single non-looping clip through the Animator's Motion Time parameter.
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class HoldDoorAction : MonoBehaviour, IJsonSaveable
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AnimationClip openingClip;
    [SerializeField, Min(0.01f)] private float playbackSpeed = 1f;
    [SerializeField, Min(1)] private int requiredSignals = 1;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openingSound;
    private int activeSignals;

    public int ActiveSignals => activeSignals;
    public bool IsPressed => activeSignals >= Mathf.Max(1, requiredSignals);
    [SerializeField, Range(0f, 1f)] private float progress;

    private static readonly int ProgressParameter = Animator.StringToHash("OpenProgress");

    [System.Serializable]
    private class SaveState
    {
        public int ActiveSignals;
        public float Progress;
    }

    private void Awake()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();

        progress = 0f;
        ApplyProgress();
    }

    private void Update()
    {
        if (doorAnimator == null || openingClip == null || openingClip.length <= 0f)
            return;

        // Changing the target never resets progress, even during rapid reversals.
        progress = Mathf.MoveTowards(progress, IsPressed ? 1f : 0f,
            Time.deltaTime * Mathf.Max(0.01f, playbackSpeed) / openingClip.length);
        ApplyProgress();
    }

    [ContextMenu("Open")]
    public void Open()
    {
        bool wasPressed = IsPressed;
        activeSignals++;

        if (!wasPressed && IsPressed && audioSource != null && openingSound != null)
            audioSource.PlayOneShot(openingSound);
    }

    [ContextMenu("Close")]
    public void Close()
    {
        activeSignals = Mathf.Max(0, activeSignals - 1);
    }

    public string CaptureJsonState()
    {
        return JsonUtility.ToJson(new SaveState
        {
            ActiveSignals = activeSignals,
            Progress = progress
        });
    }

    public void RestoreJsonState(string json)
    {
        SaveState state = JsonUtility.FromJson<SaveState>(json);

        if (state == null)
            return;

        activeSignals = Mathf.Max(0, state.ActiveSignals);
        progress = Mathf.Clamp01(state.Progress);
        ApplyProgress();
    }

    private void OnDisable()
    {
        activeSignals = 0;
    }

    private void ApplyProgress()
    {
        if (doorAnimator != null)
            doorAnimator.SetFloat(ProgressParameter, progress);
    }
}
