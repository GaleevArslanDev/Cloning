using UnityEngine;

public class CloneClimbInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ClonePlayback playback;
    [SerializeField] private Transform mountPoint;

    private PlayerClimber rider;

    public bool CanInteract => playback != null;
    public bool RecordInteraction => false;

    private void Awake()
    {
        if (playback == null)
            playback = GetComponentInParent<ClonePlayback>();
        if (mountPoint == null)
            mountPoint = transform;
    }

    public void BeginInteraction(GameObject interactor)
    {
        if (!CanInteract)
            return;

        PlayerClimber climber = interactor.GetComponent<PlayerClimber>();

        if (climber == null)
            return;

        rider = climber;
        rider.Mount(playback, mountPoint);
    }

    public void Interact(GameObject interactor, float deltaTime)
    {
    }

    public void EndInteraction(GameObject interactor)
    {
    }

    private void OnDestroy()
    {
        if (rider != null)
            rider.DismountFrom(playback);
    }
}
