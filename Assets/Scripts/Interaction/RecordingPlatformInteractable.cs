using UnityEngine;

public class RecordingPlatformInteractable : MonoBehaviour, IInteractable
{
    public bool CanInteract => enabled && gameObject.activeInHierarchy;
    public bool RecordInteraction => false;

    public void BeginInteraction(GameObject interactor)
    {
        CloneRecorder recorder = interactor.GetComponent<CloneRecorder>();

        if (recorder != null)
            recorder.FinishRecording();
    }

    public void Interact(GameObject interactor, float deltaTime)
    {
    }

    public void EndInteraction(GameObject interactor)
    {
    }
}
