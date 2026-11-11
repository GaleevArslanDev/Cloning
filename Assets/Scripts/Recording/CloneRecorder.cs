using System.Collections.Generic;
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
    [SerializeField] private CarryableItem[] resettableItems;
    [SerializeField] private float sampleInterval = 0.05f;
    [SerializeField] private int maximumClones = 5;

    private readonly List<CloneRecordingData> savedRecordings = new List<CloneRecordingData>();
    private readonly List<ClonePlayback> activeClones = new List<ClonePlayback>();
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

        if (resettableItems == null || resettableItems.Length == 0)
        {
            resettableItems = FindObjectsByType<CarryableItem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        }

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

        SaveRecording();
        RestartClones();
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

    private void SaveRecording()
    {
        if (recording.Poses.Count == 0)
            return;

        savedRecordings.Add(recording);

        int cloneLimit = maximumClones > 0 ? maximumClones : 5;

        while (savedRecordings.Count > cloneLimit)
            savedRecordings.RemoveAt(0);
    }

    private void RestartClones()
    {
        StopActiveClones();
        ResetItems();

        foreach (CloneRecordingData savedRecording in savedRecordings)
            SpawnClone(savedRecording);
    }

    private void SpawnClone(CloneRecordingData recordingToPlay)
    {
        if (clonePrefab == null || recordingToPlay.Poses.Count == 0)
            return;

        GameObject clone = Instantiate(clonePrefab);
        ClonePlayback playback = clone.GetComponent<ClonePlayback>();

        if (playback == null)
        {
            Destroy(clone);
            return;
        }

        playback.Initialize(recordingToPlay);
        activeClones.Add(playback);
    }

    private void StopActiveClones()
    {
        foreach (ClonePlayback clone in activeClones)
        {
            if (clone != null)
                clone.StopPlayback();
        }

        activeClones.Clear();
    }

    private void ResetItems()
    {
        foreach (CarryableItem item in resettableItems)
        {
            if (item != null)
                item.ResetToStart();
        }

        Physics.SyncTransforms();
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
        StopActiveClones();

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
