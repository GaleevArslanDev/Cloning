using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-10000)]
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject savePanel;
    [SerializeField] private LoadSaveMenu loadMenu;
    [SerializeField] private InputField saveNameInput;
    [SerializeField] private Text saveMessage;
    [SerializeField] private Color errorColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private Color successColor = new Color(0.4f, 0.85f, 0.5f);

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
        SceneManager.sceneLoaded -= InstallForScene;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded += InstallForScene;
    }

    private static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu" || FindFirstObjectByType<PauseMenuController>() != null)
            return;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<PlayerMovement>(true) == null) continue;
            var menu = new GameObject("PauseMenu");
            SceneManager.MoveGameObjectToScene(menu, scene);
            menu.AddComponent<PauseMenuController>();
            break;
        }
    }

    private void Awake()
    {
        if (menuRoot == null)
        {
            menuRoot = PauseMenuView.Create(transform, Resume, RestartLevel, ExitGame);
            pausePanel = menuRoot.transform.Find("PausePanel").gameObject;
        }
        SetMenuVisible(false);
    }

    private void Update()
    {
        if (loading || Mouse.current == null || !Mouse.current.middleButton.wasPressedThisFrame)
            return;

        if (!isPaused)
        {
            Pause();
            return;
        }

        if (pausePanel != null && pausePanel.activeSelf)
            Resume();
        else
            ShowPausePanel();
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

        SetMenuVisible(true);
        ShowPausePanel();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (EventSystem.current != null && pausePanel != null)
        {
            var firstButton = pausePanel.GetComponentInChildren<Button>();
            if (firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
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
        SetMenuVisible(false);
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
        SetMenuVisible(false);
    }

    public void ShowPausePanel()
    {
        SetPanel(pausePanel, true);
        SetPanel(savePanel, false);

        if (loadMenu != null)
            loadMenu.Close();
    }

    public void ShowSavePanel()
    {
        SetPanel(pausePanel, false);
        SetPanel(savePanel, true);

        if (loadMenu != null)
            loadMenu.Close();

        if (saveNameInput != null)
        {
            saveNameInput.text = string.Empty;
            saveNameInput.ActivateInputField();
        }

        SetSaveMessage(string.Empty, errorColor);
    }

    public void ShowLoadPanel()
    {
        SetPanel(pausePanel, false);
        SetPanel(savePanel, false);

        if (loadMenu != null)
            loadMenu.Open();
    }

    public void SaveGame()
    {
        string slotName = saveNameInput != null ? saveNameInput.text.Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(slotName))
        {
            SetSaveMessage("Введите название сохранения", errorColor);
            return;
        }

        if (string.Equals(slotName, "Autosave", StringComparison.OrdinalIgnoreCase))
        {
            SetSaveMessage("Это название занято автосохранением", errorColor);
            return;
        }

        if (GameSaveSystem.Exists(slotName))
        {
            SetSaveMessage("Сохранение с таким названием уже существует", errorColor);
            return;
        }

        if (!GameSaveSystem.SaveNew(slotName))
        {
            SetSaveMessage("Не удалось сохранить игру", errorColor);
            return;
        }

        SetSaveMessage("Игра сохранена", successColor);
    }

    public void RestartLevel()
    {
        LoadScene(SceneManager.GetActiveScene().path);
    }

    public void ExitGame()
    {
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

    private void SetMenuVisible(bool visible)
    {
        if (menuRoot != null)
            menuRoot.SetActive(visible);
    }

    private void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private void SetSaveMessage(string message, Color color)
    {
        if (saveMessage == null)
            return;

        saveMessage.color = color;
        saveMessage.text = message;
    }
}
