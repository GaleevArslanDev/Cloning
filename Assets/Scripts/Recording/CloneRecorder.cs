using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField, Min(0.02f)] private float sampleInterval = 0.05f;
    [SerializeField, Range(1, 100)] private int maximumClones = 5;

    private readonly List<CloneRecordingData> savedRecordings = new List<CloneRecordingData>();
    private readonly List<ClonePlayback> activeClones = new List<ClonePlayback>();
    private CloneRecordingData recording;
    private float recordingTime;
    private float nextSampleTime;
    private bool isRecording;
    private bool previousLeftInput;

    private void Awake()
    {
        if (FindFirstObjectByType<AutosaveController>() == null)
            gameObject.AddComponent<AutosaveController>();

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
        if (FindFirstObjectByType<LevelManager>() != null)
            return;

        MovePlayerToStart();
        BeginRecording();
    }

    private void Update()
    {
        if (PauseMenuController.IsGamePaused) return;

        if (!isRecording)
            return;

        recordingTime += Time.deltaTime;
        RecordInput();

        if (recordingTime >= nextSampleTime)
        {
            RecordPose();
            nextSampleTime = recordingTime + GetSampleInterval();
        }
    }

#if UNITY_EDITOR
    public object MenuReplayCycle => recording;
#endif

    public void FinishRecording()
    {
        if (!isRecording || recording == null)
            return;

        RecordPose();
        recording.Duration = recordingTime;
        isRecording = false;

        SaveRecording();
        RestartClones();
        MovePlayerToStart();
        BeginRecording();
    }

    public void EndCurrentLevel()
    {
        isRecording = false;
        recording = null;
        savedRecordings.Clear();
        StopActiveClones();

        if (playerCarry != null)
            playerCarry.CancelCarry();
        if (playerClimber != null)
            playerClimber.Dismount();
    }

    public void StartNewLevel(Transform levelStartPoint)
    {
        PrepareNewLevel(levelStartPoint);
        BeginPreparedLevel();
    }

    public void PrepareNewLevel(Transform levelStartPoint)
    {
        isRecording = false;
        recording = null;
        savedRecordings.Clear();
        StopActiveClones();

        if (levelStartPoint != null)
            startPoint = levelStartPoint;

        if (playerCarry != null)
            playerCarry.CancelCarry();
        if (playerClimber != null)
            playerClimber.Dismount();

        CaptureLevelItems();
        MovePlayerToStart();
    }

    public void BeginPreparedLevel()
    {
        if (isRecording)
            return;

        BeginRecording();
    }

    public CloneRecorderSaveData CaptureSaveData()
    {
        if (isRecording && recording != null)
        {
            RecordPose();
            recording.Duration = recordingTime;
        }

        CloneRecorderSaveData save = new CloneRecorderSaveData
        {
            RecordingTime = recordingTime,
            IsRecording = isRecording,
            CurrentRecording = RecordingSaveConverter.ToSaveData(recording)
        };

        foreach (CloneRecordingData savedRecording in savedRecordings)
        {
            RecordingSaveData savedData = RecordingSaveConverter.ToSaveData(savedRecording);

            if (savedData != null)
                save.SavedRecordings.Add(savedData);
        }

        return save;
    }

    public void RestoreSaveData(CloneRecorderSaveData save)
    {
        if (save == null)
            return;

        List<CloneRecordingData> restoredRecordings = new List<CloneRecordingData>();

        if (save.SavedRecordings != null)
        {
            foreach (RecordingSaveData savedRecording in save.SavedRecordings)
            {
                CloneRecordingData restored = RecordingSaveConverter.FromSaveData(savedRecording);

                if (restored != null && restored.Poses.Count > 0)
                    restoredRecordings.Add(restored);
            }
        }

        int cloneLimit = GetCloneLimit();

        while (restoredRecordings.Count > cloneLimit)
            restoredRecordings.RemoveAt(0);

        CloneRecordingData restoredCurrent = RecordingSaveConverter.FromSaveData(
            save.CurrentRecording
        );

        if (save.IsRecording && restoredCurrent == null)
            restoredCurrent = new CloneRecordingData();

        float restoredTime = Mathf.Max(
            0f,
            SaveDataValidator.Finite(save.RecordingTime, 0f)
        );

        StopActiveClones();
        savedRecordings.Clear();
        savedRecordings.AddRange(restoredRecordings);
        recording = restoredCurrent;
        recordingTime = restoredTime;
        nextSampleTime = recordingTime + GetSampleInterval();
        isRecording = save.IsRecording && recording != null;
        previousLeftInput = Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (isRecording && recording.Poses.Count == 0)
            RecordPose();

        foreach (CloneRecordingData savedRecording in savedRecordings)
            SpawnClone(savedRecording, recordingTime);
    }

    private void BeginRecording()
    {
        recording = new CloneRecordingData();
        recordingTime = 0f;
        nextSampleTime = GetSampleInterval();
        isRecording = true;
        previousLeftInput = Mouse.current != null && Mouse.current.leftButton.isPressed;
        RecordPose();
    }

    private void RecordInput()
    {
        if (recording == null || Mouse.current == null)
            return;

        bool leftInput = Mouse.current.leftButton.isPressed;

        if (leftInput != previousLeftInput)
        {
            recording.InputActions.Add(new RecordedInputAction
            {
                Time = recordingTime,
                Type = leftInput ? RecordedInputType.LeftPressed : RecordedInputType.LeftReleased
            });

            previousLeftInput = leftInput;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            recording.InputActions.Add(new RecordedInputAction
            {
                Time = recordingTime,
                Type = RecordedInputType.RightPressed
            });
        }

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) >= 0.01f)
        {
            recording.InputActions.Add(new RecordedInputAction
            {
                Time = recordingTime,
                Type = scroll > 0f
                    ? RecordedInputType.ScrollUp
                    : RecordedInputType.ScrollDown
            });
        }
    }

    private void RecordPose()
    {
        if (recording == null || playerBody == null || playerHead == null)
            return;

        Vector3 carryLocalPosition = Vector3.zero;
        Quaternion carryLocalRotation = Quaternion.identity;
        bool hasCarryPose = playerCarry != null && playerCarry.TryGetHeldPose(
            playerBody,
            out carryLocalPosition,
            out carryLocalRotation
        );

        recording.Poses.Add(new RecordedPose
        {
            Time = recordingTime,
            Position = playerBody.position,
            BodyRotation = playerBody.rotation,
            HeadRotation = playerHead.localRotation,
            IsJumping = playerClimber != null && playerClimber.IsJumping,
            IsRidingClone = playerClimber != null &&
                playerClimber.IsMounted &&
                !playerClimber.IsClimbing,
            HasCarryPose = hasCarryPose,
            CarryLocalPosition = carryLocalPosition,
            CarryLocalRotation = carryLocalRotation
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
        if (!isRecording || recording == null ||
            interactable == null || !interactable.RecordInteraction)
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
        if (!isRecording || recording == null)
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
        if (!isRecording || recording == null)
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
        if (recording == null || recording.Poses.Count == 0)
            return;

        savedRecordings.Add(recording);

        int cloneLimit = GetCloneLimit();

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
        SpawnClone(recordingToPlay, 0f);
    }

    private void SpawnClone(CloneRecordingData recordingToPlay, float startTime)
    {
        if (clonePrefab == null ||
            recordingToPlay == null ||
            recordingToPlay.Poses.Count == 0 ||
            startTime >= recordingToPlay.Duration)
        {
            return;
        }

        GameObject clone = Instantiate(clonePrefab);
        ClonePlayback playback = clone.GetComponent<ClonePlayback>();

        if (playback == null)
        {
            Destroy(clone);
            return;
        }

        playback.Initialize(recordingToPlay, startTime);
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

    private void CaptureLevelItems()
    {
        resettableItems = FindObjectsByType<CarryableItem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (CarryableItem item in resettableItems)
        {
            if (item != null)
                item.CaptureStartState();
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

    private float GetSampleInterval()
    {
        return Mathf.Clamp(
            SaveDataValidator.Finite(sampleInterval, 0.05f),
            0.02f,
            10f
        );
    }

    private int GetCloneLimit()
    {
        return Mathf.Clamp(maximumClones > 0 ? maximumClones : 5, 1, 100);
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
