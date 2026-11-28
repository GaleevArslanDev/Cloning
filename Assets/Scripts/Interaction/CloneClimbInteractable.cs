using UnityEngine;

public class CloneClimbInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ClonePlayback playback;
    [SerializeField] private Transform mountPoint;
    [SerializeField, Min(0f)] private float stackHorizontalDistance = 0.75f;
    [SerializeField, Min(0f)] private float minimumStackHeight = 1.2f;
    [SerializeField, Min(0f)] private float maximumStackHeight = 2.6f;

    private PlayerClimber rider;

    public bool CanInteract => playback != null && !HasCloneBelow();
    public bool RecordInteraction => false;

    private void Awake()
    {
        stackHorizontalDistance = Mathf.Max(
            0f,
            SaveDataValidator.Finite(stackHorizontalDistance, 0.75f)
        );
        minimumStackHeight = Mathf.Max(
            0f,
            SaveDataValidator.Finite(minimumStackHeight, 1.2f)
        );
        maximumStackHeight = Mathf.Max(
            minimumStackHeight,
            SaveDataValidator.Finite(maximumStackHeight, 2.6f)
        );

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
