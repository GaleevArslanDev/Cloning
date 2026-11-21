using UnityEngine;

public class AutosaveController : MonoBehaviour
{
    private static bool exitSaveCompleted;

    [SerializeField] private float interval = 300f;
    [SerializeField] private string slotName = "Autosave";

    private float elapsedTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetExitState()
    {
        exitSaveCompleted = false;
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

        elapsedTime = 0f;
        GameSaveSystem.Save(slotName);
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
        string saveName = controller != null ? controller.slotName : "Autosave";
        GameSaveSystem.Save(saveName);
    }
}
