using UnityEngine;

public class RecordingPlatformInteractable : MonoBehaviour, IInteractable
{
    private LevelManager levelManager;

    public bool CanInteract => enabled && gameObject.activeInHierarchy;
    public bool RecordInteraction => false;

    private void Awake()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    public void BeginInteraction(GameObject interactor)
    {
        CloneRecorder recorder = interactor.GetComponent<CloneRecorder>();

        if (recorder == null)
            return;

        if (levelManager != null && levelManager.TryCompleteFromStart(interactor))
            return;

        recorder.FinishRecording();
    }

    public void Interact(GameObject interactor, float deltaTime)
    {
    }

    public void EndInteraction(GameObject interactor)
    {
    }
}
