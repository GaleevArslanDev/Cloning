using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarryableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Vector3 holdPositionOffset;
    [SerializeField] private Vector3 holdRotationOffset = new Vector3(8f, -12f, 4f);

    private Rigidbody body;
    private Collider[] itemColliders;
    private bool[] originalTriggers;
    private bool originalKinematic;
    private bool originalGravity;
    private bool startKinematic;
    private bool startGravity;
    private bool[] startTriggers;
    private Transform startParent;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private GameObject owner;
    private bool initialized;

    public bool CanInteract => owner == null && enabled && gameObject.activeInHierarchy;
    public bool RecordInteraction => false;
    public Vector3 HoldPositionOffset => holdPositionOffset;
    public Vector3 HoldRotationOffset => holdRotationOffset;
    public bool IsHeld => owner != null;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        body = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>();
        originalTriggers = new bool[itemColliders.Length];
        startTriggers = new bool[itemColliders.Length];
        startParent = transform.parent;
        startPosition = transform.position;
        startRotation = transform.rotation;
        startKinematic = body.isKinematic;
        startGravity = body.useGravity;

        for (int i = 0; i < itemColliders.Length; i++)
            startTriggers[i] = itemColliders[i].isTrigger;
    }

    public void BeginInteraction(GameObject interactor)
    {
        PlayerCarry playerCarry = interactor.GetComponent<PlayerCarry>();

        if (playerCarry != null)
            playerCarry.TryPickup(this);
    }

    public void Interact(GameObject interactor, float deltaTime)
    {
    }

    public void EndInteraction(GameObject interactor)
    {
    }

    public bool TryTake(GameObject newOwner)
    {
        Initialize();

        if (newOwner == null || owner != null)
            return false;

        owner = newOwner;
        originalKinematic = body.isKinematic;
        originalGravity = body.useGravity;

        body.isKinematic = true;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        for (int i = 0; i < itemColliders.Length; i++)
        {
            originalTriggers[i] = itemColliders[i].isTrigger;
            itemColliders[i].isTrigger = true;
        }

        return true;
    }

    public void Release(GameObject currentOwner, Vector3 position, Quaternion rotation)
    {
        if (owner != currentOwner)
            return;

        transform.SetPositionAndRotation(position, rotation);

        for (int i = 0; i < itemColliders.Length; i++)
            itemColliders[i].isTrigger = originalTriggers[i];

        body.isKinematic = originalKinematic;
        body.useGravity = originalGravity;
        owner = null;
    }

    public Bounds GetBounds()
    {
        if (itemColliders.Length == 0)
            return new Bounds(transform.position, Vector3.one * 0.2f);

        Bounds bounds = itemColliders[0].bounds;

        for (int i = 1; i < itemColliders.Length; i++)
            bounds.Encapsulate(itemColliders[i].bounds);

        return bounds;
    }

    public void ResetToStart()
    {
        Initialize();

        owner = null;
        body.isKinematic = true;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        transform.SetParent(startParent);
        transform.SetPositionAndRotation(startPosition, startRotation);

        for (int i = 0; i < itemColliders.Length; i++)
            itemColliders[i].isTrigger = startTriggers[i];

        body.useGravity = startGravity;
        body.isKinematic = startKinematic;
    }
}
