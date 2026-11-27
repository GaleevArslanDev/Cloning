using UnityEngine;
using UnityEngine.Events;

public class LevelStage : MonoBehaviour
{
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private string nextSceneName;
    [SerializeField, Min(0f)] private float finishAnimationDuration = 2f;
    [SerializeField] private bool waitForFinishAnimationSignal;
    [SerializeField, Min(0.05f)] private float finishAnimationSignalTimeout = 10f;
    [SerializeField, Min(0f)] private float transitionDuration = 2f;
    [SerializeField] private UnityEvent onLevelStarted;
    [SerializeField] private UnityEvent onFinishAnimationStarted;
    [SerializeField] private UnityEvent onFinishAnimationFinished;
    [SerializeField] private UnityEvent onTransitionStarted;
    [SerializeField] private UnityEvent onTransitionFinished;

    public Transform PlayerStartPoint => playerStartPoint;
    public string NextSceneName => nextSceneName;
    public float FinishAnimationDuration => Mathf.Max(
        0f,
        Mathf.Min(300f, SaveDataValidator.Finite(finishAnimationDuration, 0f))
    );
    public bool WaitForFinishAnimationSignal => waitForFinishAnimationSignal;
    public float FinishAnimationSignalTimeout => Mathf.Max(
        0.05f,
        Mathf.Min(300f, SaveDataValidator.Finite(finishAnimationSignalTimeout, 10f))
    );
    public float TransitionDuration => Mathf.Max(
        0f,
        Mathf.Min(300f, SaveDataValidator.Finite(transitionDuration, 0f))
    );

    public void StartLevel()
    {
        onLevelStarted?.Invoke();
    }

    public void StartTransition()
    {
        onTransitionStarted?.Invoke();
    }

    public void StartFinishAnimation()
    {
        onFinishAnimationStarted?.Invoke();
    }

    public void FinishFinishAnimation()
    {
        onFinishAnimationFinished?.Invoke();
    }

    public void FinishTransition()
    {
        onTransitionFinished?.Invoke();
    }
}
