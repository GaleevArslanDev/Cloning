using UnityEngine;

public class CloneClimbInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ClonePlayback playback;
    [SerializeField] private Transform mountPoint;
    [SerializeField] private float stackHorizontalDistance = 0.75f;
    [SerializeField] private float minimumStackHeight = 1.2f;
    [SerializeField] private float maximumStackHeight = 2.6f;

    private PlayerClimber rider;

    public bool CanInteract => playback != null && !HasCloneBelow();
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

    private bool HasCloneBelow()
    {
        return playback != null && playback.HasCloneBelow(
            stackHorizontalDistance,
            minimumStackHeight,
            maximumStackHeight
        );
    }
}
