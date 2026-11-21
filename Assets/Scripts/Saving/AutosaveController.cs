using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class AutosaveController : MonoBehaviour
{
    private static bool exitSaveCompleted;
    private static float elapsedTime;

    [SerializeField] private float interval = 300f;
    [FormerlySerializedAs("slotName")]
    [SerializeField] private string slotNamePrefix = "Autosave";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetExitState()
    {
        exitSaveCompleted = false;
        elapsedTime = 0f;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        elapsedTime += Time.unscaledDeltaTime;

        if (elapsedTime < Mathf.Max(1f, interval))
            return;

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager != null && levelManager.IsTransitioning)
            return;

        if (GameSaveSystem.Save(CreateSaveName()))
            elapsedTime = 0f;
    }

    private void OnApplicationQuit()
    {
        SaveBeforeExit();
    }

    public static void SaveBeforeExit()
    {
        if (exitSaveCompleted)
            return;

        exitSaveCompleted = true;
        AutosaveController controller = FindFirstObjectByType<AutosaveController>();
        string saveName = controller != null
            ? controller.CreateSaveName()
            : CreateSaveName("Autosave");
        GameSaveSystem.Save(saveName);
    }

    private string CreateSaveName()
    {
        return CreateSaveName(slotNamePrefix);
    }

    private static string CreateSaveName(string prefix)
    {
        string levelNumber = GameSaveSystem.GetLevelNumber(
            SceneManager.GetActiveScene().name
        );
        string date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        return $"{prefix}_Level_{levelNumber}_{date}";
    }
}
