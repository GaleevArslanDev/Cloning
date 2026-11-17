using UnityEngine;

public class FloorCubeLiftAction : MonoBehaviour
{
    [SerializeField] private RoomCubeField roomCubes;
    [SerializeField] private Transform[] floorCubes;
    [SerializeField] private float liftHeight = 2f;
    [SerializeField] private float movementSpeed = 1f;

    private int activeRequests;
    private float currentHeight;
    private bool registered;

    public bool IsActive => activeRequests > 0;

    private void Start()
    {
        if (roomCubes == null)
            roomCubes = FindFirstObjectByType<RoomCubeField>();

        Register();
    }

    private void FixedUpdate()
    {
        if (!registered)
            Register();

        float targetHeight = IsActive ? Mathf.Max(0f, liftHeight) : 0f;
        currentHeight = Mathf.MoveTowards(
            currentHeight,
            targetHeight,
            Mathf.Max(0f, movementSpeed) * Time.fixedDeltaTime
        );

        if (roomCubes != null)
            roomCubes.SetFloorLift(this, currentHeight);
    }

    public void Activate()
    {
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

    private void Register()
    {
        if (registered || roomCubes == null)
            return;

        roomCubes.RegisterFloorLift(this, floorCubes);
        registered = true;
    }

    private void OnDisable()
    {
        activeRequests = 0;
        currentHeight = 0f;

        if (roomCubes != null)
            roomCubes.SetFloorLift(this, 0f);
    }

    private void OnDestroy()
    {
        if (roomCubes != null)
            roomCubes.RemoveFloorLift(this);
    }

}
