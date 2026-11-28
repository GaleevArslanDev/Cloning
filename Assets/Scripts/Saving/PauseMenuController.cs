using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-10000)]
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button firstButton;

    private float previousTimeScale = 1f;
    private bool isPaused;
    private bool resumeRequested;
    private bool loading;
    private bool previousAudioPause;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    public static bool IsGamePaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsGamePaused = false;
    }

    private void Awake()
    {
        menuRoot.SetActive(false);
    }

    private void Update()
    {
        if (loading || gameObject.scene != SceneManager.GetActiveScene() ||
            Mouse.current == null || !Mouse.current.middleButton.wasPressedThisFrame)
            return;

        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (isPaused || loading)
            return;

        isPaused = true;
        IsGamePaused = true;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        menuRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (EventSystem.current != null && firstButton != null)
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    public void Resume()
    {
        if (!isPaused || loading)
            return;
        // Restore after every gameplay Update, so the UI click cannot reach gameplay.
        resumeRequested = true;
    }

    private void LateUpdate()
    {
        if (!resumeRequested) return;
        resumeRequested = false;
        RestorePauseState();
        if (menuRoot != null) menuRoot.SetActive(false);
    }

    private void RestorePauseState()
    {
        if (!isPaused) return;
        isPaused = false;
        IsGamePaused = false;
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void OnDisable()
    {
        resumeRequested = false;
        RestorePauseState();
        if (menuRoot != null) menuRoot.SetActive(false);
    }

    public void RestartLevel()
    {
        LoadScene(SceneManager.GetActiveScene().path);
    }

    public void ExitGame()
    {
        AutosaveController.SaveBeforeExit();
        LoadScene("MainMenu");
    }

    private void LoadScene(string scene)
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Scene is missing from the build scene list: " + scene, this);
            return;
        }
        loading = true;
        resumeRequested = false;
        RestorePauseState();
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}
