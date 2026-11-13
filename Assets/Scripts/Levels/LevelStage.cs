using UnityEngine;
using UnityEngine.Events;

public class LevelStage : MonoBehaviour
{
    [SerializeField] private Transform playerStartPoint;
    [SerializeField] private float transitionDuration = 2f;
    [SerializeField] private UnityEvent onLevelStarted;
    [SerializeField] private UnityEvent onTransitionStarted;
    [SerializeField] private UnityEvent onTransitionFinished;

    public Transform PlayerStartPoint => playerStartPoint;
    public float TransitionDuration => Mathf.Max(0f, transitionDuration);

    public void StartLevel()
    {
        onLevelStarted?.Invoke();
    }

    public void StartTransition()
    {
        onTransitionStarted?.Invoke();
    }

    public void FinishTransition()
    {
        onTransitionFinished?.Invoke();
    }
}
