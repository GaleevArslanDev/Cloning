using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuController : MonoBehaviour
{
    private bool loading;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void NewGame()
    {
        if (loading) return;
        const string firstLevel = "Level1";
        if (!Application.CanStreamedLevelBeLoaded(firstLevel))
        {
            Debug.LogError("Add Level1 to the build scene list.", this);
            return;
        }
        loading = true;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(firstLevel, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
