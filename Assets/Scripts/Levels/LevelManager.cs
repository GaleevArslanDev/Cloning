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
    private bool finishAnimationSignalReceived;
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

        StartCoroutine(CompleteLevel(null, null, 0f, 0f));
        return true;
    }

    public void ReachFinish()
    {
        if (isTransitioning || level == null)
            return;

        StartCoroutine(CompleteLevel(null, null, 0f, 0f));
    }

    public void ReachFinish(
        PlayerMovement player,
        Transform centerPoint,
        float centeringSpeed,
        float maximumCenteringTime
    )
    {
        if (isTransitioning || level == null)
            return;

        StartCoroutine(CompleteLevel(
            player,
            centerPoint,
            centeringSpeed,
            maximumCenteringTime
        ));
    }

    public void ContinueAfterFinishAnimation()
    {
        finishAnimationSignalReceived = true;
    }

    private IEnumerator CompleteLevel(
        PlayerMovement player,
        Transform centerPoint,
        float centeringSpeed,
        float maximumCenteringTime
    )
    {
        isTransitioning = true;

        if (cloneRecorder != null)
            cloneRecorder.EndCurrentLevel();
        if (playerControl != null)
            playerControl.SetTransitionControl();

        if (player != null && centerPoint != null)
        {
            float elapsed = 0f;
            float maximumTime = Mathf.Max(0.01f, maximumCenteringTime);
            Vector3 targetPosition = centerPoint.position;
            targetPosition.y = player.transform.position.y;

            while (elapsed < maximumTime &&
                   !player.MoveTo(targetPosition, centeringSpeed, 0.02f))
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            player.Teleport(targetPosition, player.transform.rotation);
        }

        finishAnimationSignalReceived = false;
        level.StartFinishAnimation();

        if (level.WaitForFinishAnimationSignal)
        {
            while (!finishAnimationSignalReceived)
                yield return null;
        }
        else if (level.FinishAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(level.FinishAnimationDuration);
        }

        level.FinishFinishAnimation();
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
