using UnityEngine;

// Scrubs a single non-looping clip through the Animator's Motion Time parameter.
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class HoldDoorAction : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AnimationClip openingClip;
    [SerializeField, Min(0.01f)] private float playbackSpeed = 1f;
    [SerializeField] private bool isPressed;
    [SerializeField, Range(0f, 1f)] private float progress;

    private static readonly int ProgressParameter = Animator.StringToHash("OpenProgress");

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
        progress = Mathf.MoveTowards(progress, isPressed ? 1f : 0f,
            Time.deltaTime * Mathf.Max(0.01f, playbackSpeed) / openingClip.length);
        ApplyProgress();
    }

    [ContextMenu("Open")]
    public void Open()
    {
        isPressed = true;
    }

    [ContextMenu("Close")]
    public void Close()
    {
        isPressed = false;
    }

    private void OnDisable()
    {
        isPressed = false;
    }

    private void ApplyProgress()
    {
        if (doorAnimator != null)
            doorAnimator.SetFloat(ProgressParameter, progress);
    }
}
