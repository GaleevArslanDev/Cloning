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
    private AudioSource transitionAudioSource;
    private AudioClip flaskClosingSound;
    private AudioClip flaskOpeningSound;
    [FormerlySerializedAs("onAllLevelsCompleted")]
    [SerializeField] private UnityEvent onLevelCompleted;

    private bool isTransitioning;
    private bool levelConditionCompleted;
    private bool finishAnimationSignalReceived;
    private static bool enteringFromTransition;
    private static bool enteringFromSave;
    private static bool hasPreservedLook;
    private static Quaternion preservedBodyRotation;
    private static float preservedPitch;

    public bool IsTransitioning => isTransitioning;
    public bool IsReady { get; private set; }
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

        transitionAudioSource = PlayerAudioController.GetOneShotSource();
        GameAudioClips audioClips = GameAudioClips.Instance;

        if (audioClips != null)
        {
            flaskClosingSound = audioClips.FlaskClosing;
            flaskOpeningSound = audioClips.FlaskOpening;
        }
    }

    private IEnumerator Start()
    {
        if (level == null)
        {
            IsReady = true;
            yield break;
        }

        if (playerControl != null)
            playerControl.SetTransitionControl();

        level.StartLevel();

        if (cloneRecorder != null)
            cloneRecorder.PrepareNewLevel(level.PlayerStartPoint);

        bool cameFromTransition = enteringFromTransition;
        bool cameFromSave = enteringFromSave;
        enteringFromSave = false;

        if (!cameFromSave && cameFromTransition && hasPreservedLook)
        {
            FirstPersonCamera playerCamera = FindFirstObjectByType<FirstPersonCamera>();

            if (playerCamera != null)
                playerCamera.SetLook(preservedBodyRotation, preservedPitch);

            hasPreservedLook = false;
        }

        bool shouldBuild = !cameFromSave &&
                           (cameFromTransition ||
                            roomTransition != null && roomTransition.BuildWhenSceneStarts);
        enteringFromTransition = false;

        if (shouldBuild && roomTransition != null)
            yield return roomTransition.BuildLevel();

        if (cloneRecorder != null)
            cloneRecorder.BeginPreparedLevel();

        if (playerControl != null)
            playerControl.SetControlEnabled(true);

        if (cameFromTransition)
            PlayTransitionSound(flaskOpeningSound);

        IsReady = true;
    }

    public static void PrepareForSaveLoad()
    {
        enteringFromSave = true;
        enteringFromTransition = false;
        hasPreservedLook = false;
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

    public void RestoreLevelCondition(bool completed)
    {
        levelConditionCompleted = completed;
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

        PlayTransitionSound(flaskClosingSound);

        if (player != null && centerPoint != null)
        {
            float elapsed = 0f;
            float maximumTime = Mathf.Max(
                0.01f,
                SaveDataValidator.Finite(maximumCenteringTime, 0.01f)
            );
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
            float signalWait = 0f;

            while (!finishAnimationSignalReceived &&
                   signalWait < level.FinishAnimationSignalTimeout)
            {
                signalWait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!finishAnimationSignalReceived)
            {
                Debug.LogWarning(
                    "LevelManager: сигнал завершения анимации не получен вовремя, переход продолжен",
                    this
                );
            }
        }
        else if (level.FinishAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(level.FinishAnimationDuration);
        }

        level.FinishFinishAnimation();
        string nextSceneName = level.NextSceneName != null
            ? level.NextSceneName.Trim()
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(nextSceneName) &&
            !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            onLevelCompleted?.Invoke();
            FinishWithoutScene($"сцена '{nextSceneName}' недоступна в сборке");
            yield break;
        }

        level.StartTransition();

        if (roomTransition != null)
            yield return roomTransition.HideLevel();
        else if (level.TransitionDuration > 0f)
            yield return new WaitForSeconds(level.TransitionDuration);

        level.FinishTransition();
        onLevelCompleted?.Invoke();

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            FinishWithoutScene("не указана следующая сцена");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            FinishWithoutScene($"сцена '{nextSceneName}' недоступна в сборке");
            yield break;
        }

        FirstPersonCamera currentCamera = FindFirstObjectByType<FirstPersonCamera>();

        if (currentCamera != null)
        {
            preservedBodyRotation = currentCamera.BodyRotation;
            preservedPitch = currentCamera.Pitch;
            hasPreservedLook = true;
        }

        enteringFromTransition = true;
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneName);

        if (operation == null)
        {
            enteringFromTransition = false;
            FinishWithoutScene($"не удалось начать загрузку сцены '{nextSceneName}'");
            yield break;
        }

        yield return operation;
    }

    private void PlayTransitionSound(AudioClip clip)
    {
        if (transitionAudioSource != null && clip != null)
            transitionAudioSource.PlayOneShot(clip);
    }

    private void FinishWithoutScene(string reason)
    {
        Debug.LogWarning($"LevelManager: {reason}", this);
        isTransitioning = false;

        if (playerControl != null)
            playerControl.SetControlEnabled(true);
    }
}
