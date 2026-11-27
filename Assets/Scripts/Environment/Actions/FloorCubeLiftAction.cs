using UnityEngine;
using UnityEngine.Serialization;

public class FloorCubeLiftAction : MonoBehaviour, IJsonSaveable
{
    private enum LiftSurface
    {
        Floor,
        Ceiling
    }

    [SerializeField] private RoomCubeField roomCubes;
    [SerializeField] private LiftSurface surface;
    [FormerlySerializedAs("floorCubes")]
    [SerializeField] private Transform[] cubes;
    [SerializeField, Min(0f)] private float liftHeight = 2f;
    [SerializeField, Min(0f)] private float movementSpeed = 1f;
    [SerializeField, Min(1)] private int requiredSignals = 2;
    private AudioSource movementAudioSource;
    private AudioClip movementSound;

    private int activeRequests;
    private float currentHeight;
    private bool registered;

    [System.Serializable]
    private class SaveState
    {
        public int ActiveRequests;
        public float CurrentHeight;
    }

    public bool IsActive => activeRequests >= Mathf.Max(1, requiredSignals);

    private void Start()
    {
        movementSound = GameAudioClips.Instance != null
            ? GameAudioClips.Instance.FloorMovement
            : null;

        if (movementSound != null)
            movementAudioSource = PlayerAudioController.CreatePlayerSource();

        if (roomCubes == null)
            roomCubes = FindFirstObjectByType<RoomCubeField>();

        Register();
    }

    private void FixedUpdate()
    {
        if (!registered)
            Register();

        float maximumHeight = Mathf.Clamp(
            SaveDataValidator.Finite(liftHeight, 0f),
            0f,
            10000f
        );
        float targetHeight = IsActive ? maximumHeight : 0f;
        float previousHeight = currentHeight;
        currentHeight = Mathf.MoveTowards(
            currentHeight,
            targetHeight,
            Mathf.Clamp(
                SaveDataValidator.Finite(movementSpeed, 0f),
                0f,
                100f
            ) *
            Time.fixedDeltaTime
        );

        if (roomCubes != null)
            roomCubes.SetFloorLift(this, currentHeight);

        UpdateMovementSound(!Mathf.Approximately(previousHeight, currentHeight));
    }

    private void UpdateMovementSound(bool isMoving)
    {
        if (movementAudioSource == null || movementSound == null)
            return;

        if (isMoving && !movementAudioSource.isPlaying)
        {
            movementAudioSource.clip = movementSound;
            movementAudioSource.loop = true;
            movementAudioSource.Play();
        }
        else if (!isMoving && movementAudioSource.isPlaying)
        {
            movementAudioSource.Stop();
        }
    }

    public void Activate()
    {
        if (activeRequests < int.MaxValue)
            activeRequests++;
    }

    public void Deactivate()
    {
        activeRequests = Mathf.Max(0, activeRequests - 1);
    }

    public void Raise()
    {
        Activate();
    }

    public void Lower()
    {
        Deactivate();
    }

    public string CaptureJsonState()
    {
        return JsonUtility.ToJson(new SaveState
        {
            ActiveRequests = activeRequests,
            CurrentHeight = currentHeight
        });
    }

    public void RestoreJsonState(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        SaveState state;

        try
        {
            state = JsonUtility.FromJson<SaveState>(json);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogWarning($"FloorCubeLiftAction: повреждён JSON: {exception.Message}", this);
            return;
        }

        if (state == null)
            return;

        activeRequests = Mathf.Max(0, state.ActiveRequests);
        currentHeight = Mathf.Clamp(
            SaveDataValidator.Finite(state.CurrentHeight, 0f),
            0f,
            Mathf.Clamp(
                SaveDataValidator.Finite(liftHeight, 0f),
                0f,
                10000f
            )
        );
        Register();

        if (roomCubes != null)
            roomCubes.SetFloorLift(this, currentHeight);
    }

    private void Register()
    {
        if (registered || roomCubes == null)
            return;

        roomCubes.RegisterFloorLift(
            this,
            cubes,
            surface == LiftSurface.Ceiling
        );
        registered = true;
    }

    private void OnDisable()
    {
        activeRequests = 0;
        currentHeight = 0f;

        if (roomCubes != null)
            roomCubes.SetFloorLift(this, 0f);

        if (movementAudioSource != null)
            movementAudioSource.Stop();
    }

    private void OnDestroy()
    {
        if (roomCubes != null)
            roomCubes.RemoveFloorLift(this);
    }

}
