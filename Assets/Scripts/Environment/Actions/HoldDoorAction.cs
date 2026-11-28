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
    private AudioSource audioSource;
    private AudioClip openingSound;
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
        audioSource = PlayerAudioController.GetOneShotSource();
        openingSound = GameAudioClips.Instance != null
            ? GameAudioClips.Instance.DoorOpening
            : null;

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
            Time.deltaTime * Mathf.Max(
                0.01f,
                SaveDataValidator.Finite(playbackSpeed, 1f)
            ) / openingClip.length);
        ApplyProgress();
    }

    [ContextMenu("Open")]
    public void Open()
    {
        bool wasPressed = IsPressed;

        if (activeSignals < int.MaxValue)
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
        if (string.IsNullOrWhiteSpace(json))
            return;

        SaveState state;

        try
        {
            state = JsonUtility.FromJson<SaveState>(json);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogWarning($"HoldDoorAction: повреждён JSON: {exception.Message}", this);
            return;
        }

        if (state == null)
            return;

        activeSignals = Mathf.Max(0, state.ActiveSignals);
        progress = Mathf.Clamp01(SaveDataValidator.Finite(state.Progress, 0f));
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
