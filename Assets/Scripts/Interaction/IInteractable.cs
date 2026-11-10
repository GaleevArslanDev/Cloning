using UnityEngine;

public interface IInteractable
{
    bool CanInteract { get; }
    bool RecordInteraction { get; }
    void BeginInteraction(GameObject interactor);
    void Interact(GameObject interactor, float deltaTime);
    void EndInteraction(GameObject interactor);
}
