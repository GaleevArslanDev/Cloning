using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DefaultExecutionOrder(100)]
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelStage level;
    [SerializeField] private CloneRecorder cloneRecorder;
    [SerializeField] private PlayerControlLock playerControl;
    [FormerlySerializedAs("onAllLevelsCompleted")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;
    public LevelStage Level => level;
    public string NextSceneName => level != null ? level.NextSceneName : string.Empty;

    private void Awake()
    {
        if (level == null)
            level = FindFirstObjectByType<LevelStage>();
        if (cloneRecorder == null)
            cloneRecorder = FindFirstObjectByType<CloneRecorder>();
        if (playerControl == null)
            playerControl = FindFirstObjectByType<PlayerControlLock>();
    }

    private void Start()
    {
        if (level == null)
            return;

        level.StartLevel();

        if (cloneRecorder != null)
            cloneRecorder.StartNewLevel(level.PlayerStartPoint);

        if (playerControl != null)
            playerControl.SetControlEnabled(true);
    }

    public void CompleteCurrentLevel()
    {
        if (isTransitioning || level == null)
            return;

        StartCoroutine(CompleteLevel());
    }

    private IEnumerator CompleteLevel()
    {
        isTransitioning = true;

        if (cloneRecorder != null)
            cloneRecorder.EndCurrentLevel();
        if (playerControl != null)
            playerControl.SetControlEnabled(false);

        level.StartTransition();

        if (level.TransitionDuration > 0f)
            yield return new WaitForSeconds(level.TransitionDuration);

        level.FinishTransition();
        onLevelCompleted?.Invoke();
    }
}
