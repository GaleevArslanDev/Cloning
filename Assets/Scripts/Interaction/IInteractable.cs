public interface IInteractable
{
    bool CanInteract { get; }
    void BeginInteraction();
    void Interact(float deltaTime);
    void EndInteraction();
}
