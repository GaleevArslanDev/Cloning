using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private PlayerControlLock playerControl;
    private float previousTimeScale = 1f;
    private bool isPaused;

    private void Awake()
    {
        playerControl = FindFirstObjectByType<PlayerControlLock>();
        SetMenuVisible(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
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
        if (isPaused)
            return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (playerControl == null)
            playerControl = FindFirstObjectByType<PlayerControlLock>();

        if (playerControl != null)
            playerControl.SetControlEnabled(false);

        SetMenuVisible(true);
        ShowPausePanel();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;

        if (playerControl != null)
            playerControl.SetControlEnabled(true);

        SetMenuVisible(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        AutosaveController.SaveBeforeExit();
        Time.timeScale = 1f;
        Application.Quit();
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
