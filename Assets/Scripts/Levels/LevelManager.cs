using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(100)]
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelStage[] levels;
    [SerializeField] private CloneRecorder cloneRecorder;
    [SerializeField] private PlayerControlLock playerControl;
    [SerializeField] private UnityEvent onAllLevelsCompleted;

    private int currentLevelIndex;
    private bool isTransitioning;

    public int CurrentLevelIndex => currentLevelIndex;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (cloneRecorder == null)
            cloneRecorder = FindFirstObjectByType<CloneRecorder>();
        if (playerControl == null)
            playerControl = FindFirstObjectByType<PlayerControlLock>();
    }

    private void Start()
    {
        if (levels == null || levels.Length == 0 || levels[0] == null)
            return;

        levels[0].StartLevel();

        if (cloneRecorder != null)
            cloneRecorder.StartNewLevel(levels[0].PlayerStartPoint);
    }

    public void CompleteCurrentLevel()
    {
        if (isTransitioning || levels == null || currentLevelIndex >= levels.Length)
            return;

        StartCoroutine(TransitionToNextLevel());
    }

    private IEnumerator TransitionToNextLevel()
    {
        isTransitioning = true;

        if (cloneRecorder != null)
            cloneRecorder.EndCurrentLevel();
        if (playerControl != null)
            playerControl.SetControlEnabled(false);

        LevelStage currentLevel = levels[currentLevelIndex];

        if (currentLevel != null)
        {
            currentLevel.StartTransition();

            if (currentLevel.TransitionDuration > 0f)
                yield return new WaitForSeconds(currentLevel.TransitionDuration);

            currentLevel.FinishTransition();
        }

        currentLevelIndex++;

        if (currentLevelIndex >= levels.Length)
        {
            onAllLevelsCompleted?.Invoke();
            yield break;
        }

        LevelStage nextLevel = levels[currentLevelIndex];

        if (nextLevel != null)
        {
            nextLevel.StartLevel();

            if (cloneRecorder != null)
                cloneRecorder.StartNewLevel(nextLevel.PlayerStartPoint);
        }

        if (playerControl != null)
            playerControl.SetControlEnabled(true);

        isTransitioning = false;
    }
}
