using UnityEngine;

public class AutosaveController : MonoBehaviour
{
    [SerializeField] private float interval = 300f;
    [SerializeField] private string slotName = "Autosave";

    private float elapsedTime;

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
}
