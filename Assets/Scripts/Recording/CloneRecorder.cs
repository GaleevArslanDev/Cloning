using UnityEngine;

public class CloneRecorder : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform playerHead;
    [SerializeField] private Transform startPoint;
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private FirstPersonCamera playerCamera;
    [SerializeField] private PlayerClimber playerClimber;
    [SerializeField] private PlayerCarry playerCarry;
    [SerializeField] private float sampleInterval = 0.05f;

    private CloneRecordingData recording;
    private float recordingTime;
    private float nextSampleTime;
    private bool isRecording;

    private void Awake()
    {
        if (playerBody == null)
            playerBody = transform;
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (playerInteractor == null)
            playerInteractor = GetComponent<PlayerInteractor>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<FirstPersonCamera>();
        if (playerClimber == null)
            playerClimber = GetComponent<PlayerClimber>();
        if (playerCarry == null)
            playerCarry = GetComponent<PlayerCarry>();
        if (playerHead == null && playerCamera != null)
            playerHead = playerCamera.transform;

        if (playerInteractor != null)
        {
            playerInteractor.InteractionStarted += RecordInteractionStarted;
            playerInteractor.InteractionEnded += RecordInteractionEnded;
        }

        if (playerCarry != null)
        {
            playerCarry.ItemPickedUp += RecordItemPickup;
            playerCarry.ItemDropped += RecordItemDrop;
        }
    }

    private void Start()
    {
        MovePlayerToStart();
        BeginRecording();
    }

    private void Update()
    {
        if (!isRecording)
            return;

        recordingTime += Time.deltaTime;

        if (recordingTime >= nextSampleTime)
        {
            RecordPose();
            nextSampleTime = recordingTime + Mathf.Max(0.02f, sampleInterval);
        }
    }

    public void FinishRecording()
    {
        if (!isRecording)
            return;

        RecordPose();
        recording.Duration = recordingTime;
        isRecording = false;

        SpawnClone();
        MovePlayerToStart();
        BeginRecording();
    }

    private void BeginRecording()
    {
        recording = new CloneRecordingData();
        recordingTime = 0f;
        nextSampleTime = Mathf.Max(0.02f, sampleInterval);
        isRecording = true;
        RecordPose();
    }

    private void RecordPose()
    {
        if (playerBody == null || playerHead == null)
            return;

        recording.Poses.Add(new RecordedPose
        {
            Time = recordingTime,
            Position = playerBody.position,
            BodyRotation = playerBody.rotation,
            HeadRotation = playerHead.localRotation
        });
    }

    private void RecordInteractionStarted(IInteractable interactable)
    {
        RecordInteraction(interactable, RecordedInteractionType.Begin);
    }

    private void RecordInteractionEnded(IInteractable interactable)
    {
        RecordInteraction(interactable, RecordedInteractionType.End);
    }

    private void RecordInteraction(IInteractable interactable, RecordedInteractionType type)
    {
        if (!isRecording || interactable == null || !interactable.RecordInteraction)
            return;

        MonoBehaviour target = interactable as MonoBehaviour;

        if (target == null)
            return;

        recording.Interactions.Add(new RecordedInteraction
        {
            Time = recordingTime,
            Target = target,
            Type = type
        });
    }

    private void RecordItemPickup(CarryableItem item)
    {
        if (!isRecording)
            return;

        recording.CarryActions.Add(new RecordedCarryAction
        {
            Time = recordingTime,
            Item = item,
            Type = RecordedCarryActionType.Pickup
        });
    }

    private void RecordItemDrop(CarryableItem item, Vector3 position, Quaternion rotation)
    {
        if (!isRecording)
            return;

        recording.CarryActions.Add(new RecordedCarryAction
        {
            Time = recordingTime,
            Item = item,
            Type = RecordedCarryActionType.Drop,
            DropPosition = position,
            DropRotation = rotation
        });
    }

    private void SpawnClone()
    {
        if (clonePrefab == null || recording.Poses.Count == 0)
            return;

        GameObject clone = Instantiate(clonePrefab);
        ClonePlayback playback = clone.GetComponent<ClonePlayback>();

        if (playback == null)
        {
            Destroy(clone);
            return;
        }

        playback.Initialize(recording);
    }

    private void MovePlayerToStart()
    {
        if (startPoint == null || playerMovement == null)
            return;

        if (playerClimber != null)
            playerClimber.Dismount();

        playerMovement.Teleport(startPoint.position, startPoint.rotation);

        if (playerCamera != null)
            playerCamera.ResetLook();
    }

    private void OnDestroy()
    {
        if (playerInteractor != null)
        {
            playerInteractor.InteractionStarted -= RecordInteractionStarted;
            playerInteractor.InteractionEnded -= RecordInteractionEnded;
        }

        if (playerCarry != null)
        {
            playerCarry.ItemPickedUp -= RecordItemPickup;
            playerCarry.ItemDropped -= RecordItemDrop;
        }
    }
}
