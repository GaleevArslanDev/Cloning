using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DefaultExecutionOrder(100)]
public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelStage level;
    [SerializeField] private CloneRecorder cloneRecorder;
    [SerializeField] private PlayerControlLock playerControl;
    [SerializeField] private RoomTransitionController roomTransition;
    [FormerlySerializedAs("onAllLevelsCompleted")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private bool isTransitioning;
    private bool levelConditionCompleted;
    private static bool enteringFromTransition;

    public bool IsTransitioning => isTransitioning;
    public bool LevelConditionCompleted => levelConditionCompleted;
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
        if (roomTransition == null)
            roomTransition = FindFirstObjectByType<RoomTransitionController>();
    }

    private IEnumerator Start()
    {
        if (level == null)
            yield break;

        if (playerControl != null)
            playerControl.SetTransitionControl();

        level.StartLevel();

        bool shouldBuild = enteringFromTransition ||
                           roomTransition != null && roomTransition.BuildWhenSceneStarts;
        enteringFromTransition = false;

        if (shouldBuild && roomTransition != null)
            yield return roomTransition.BuildLevel();

        if (cloneRecorder != null)
            cloneRecorder.StartNewLevel(level.PlayerStartPoint);

        if (playerControl != null)
            playerControl.SetControlEnabled(true);
    }

    public void CompleteCurrentLevel()
    {
        CompleteLevelCondition();
    }

    public void CompleteLevelCondition()
    {
        if (isTransitioning)
            return;

        levelConditionCompleted = true;
    }

    public bool TryCompleteFromStart(GameObject interactor)
    {
        if (!levelConditionCompleted || isTransitioning || level == null)
            return false;

        CloneRecorder interactorRecorder = interactor != null
            ? interactor.GetComponent<CloneRecorder>()
            : null;

        if (interactorRecorder == null ||
            cloneRecorder != null && interactorRecorder != cloneRecorder)
        {
            return false;
        }

        StartCoroutine(CompleteLevel());
        return true;
    }

    private IEnumerator CompleteLevel()
    {
        isTransitioning = true;

        if (cloneRecorder != null)
            cloneRecorder.EndCurrentLevel();
        if (playerControl != null)
            playerControl.SetTransitionControl();

        level.StartTransition();

        if (roomTransition != null)
            yield return roomTransition.HideLevel();
        else if (level.TransitionDuration > 0f)
            yield return new WaitForSeconds(level.TransitionDuration);

        level.FinishTransition();
        onLevelCompleted?.Invoke();

        if (string.IsNullOrWhiteSpace(level.NextSceneName))
            yield break;

        if (!Application.CanStreamedLevelBeLoaded(level.NextSceneName))
            yield break;

        enteringFromTransition = true;
        yield return SceneManager.LoadSceneAsync(level.NextSceneName);
    }
}
