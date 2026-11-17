using UnityEngine;
using UnityEngine.Events;

public class LevelStage : MonoBehaviour
{
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private string nextSceneName;
    [SerializeField] private float finishAnimationDuration = 2f;
    [SerializeField] private bool waitForFinishAnimationSignal;
    [SerializeField] private float transitionDuration = 2f;
    [SerializeField] private UnityEvent onLevelStarted;
    [SerializeField] private UnityEvent onFinishAnimationStarted;
    [SerializeField] private UnityEvent onFinishAnimationFinished;
    [SerializeField] private UnityEvent onTransitionStarted;
    [SerializeField] private UnityEvent onTransitionFinished;

    public Transform PlayerStartPoint => playerStartPoint;
    public string NextSceneName => nextSceneName;
    public float FinishAnimationDuration => Mathf.Max(0f, finishAnimationDuration);
    public bool WaitForFinishAnimationSignal => waitForFinishAnimationSignal;
    public float TransitionDuration => Mathf.Max(0f, transitionDuration);

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
