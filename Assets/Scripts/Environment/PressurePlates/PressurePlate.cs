using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public abstract class PressurePlate : MonoBehaviour
{
    [SerializeField] private Transform plateVisual;
    [SerializeField] private Vector3 pressedOffset = new Vector3(0f, -0.08f, 0f);
    [SerializeField] private float movementSpeed = 0.5f;
    [SerializeField] private Renderer stateRenderer;
    [SerializeField] private Color releasedColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.45f, 0.85f, 0.45f);
    [SerializeField] private float colorSpeed = 8f;
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReleased;

    private readonly Dictionary<Collider, GameObject> activeColliders = new Dictionary<Collider, GameObject>();
    private readonly Dictionary<GameObject, int> activators = new Dictionary<GameObject, int>();
    private readonly List<Collider> invalidColliders = new List<Collider>();
    private Vector3 releasedPosition;
    private Material stateMaterial;

    public bool IsPressed { get; private set; }

    protected virtual void Awake()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        Rigidbody body = GetComponent<Rigidbody>();

        trigger.isTrigger = true;
        body.isKinematic = true;
        body.useGravity = false;

        if (plateVisual == null && transform.childCount > 0)
            plateVisual = transform.GetChild(0);

        if (plateVisual != null)
            releasedPosition = plateVisual.localPosition;

        if (stateRenderer != null)
        {
            stateMaterial = stateRenderer.material;
            stateMaterial.color = releasedColor;
        }
    }

    private void Update()
    {
        RemoveInvalidColliders();
        UpdateVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        UpdateCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        UpdateCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveCollider(other);
    }

    protected abstract GameObject GetActivator(Collider other);

    private void UpdateCollider(Collider other)
    {
        GameObject newActivator = GetActivator(other);

        if (activeColliders.TryGetValue(other, out GameObject oldActivator))
        {
            if (oldActivator == newActivator)
                return;

            RemoveActivator(oldActivator);
            activeColliders.Remove(other);
        }

        if (newActivator == null)
            return;

        activeColliders.Add(other, newActivator);

        if (activators.TryGetValue(newActivator, out int colliderCount))
            activators[newActivator] = colliderCount + 1;
        else
            activators.Add(newActivator, 1);

        RefreshState();
    }

    private void RemoveCollider(Collider other)
    {
        if (!activeColliders.TryGetValue(other, out GameObject activator))
            return;

        activeColliders.Remove(other);
        RemoveActivator(activator);
    }

    private void RemoveInvalidColliders()
    {
        invalidColliders.Clear();

        foreach (KeyValuePair<Collider, GameObject> pair in activeColliders)
        {
            if (pair.Key == null ||
                pair.Value == null ||
                !pair.Key.enabled ||
                !pair.Key.gameObject.activeInHierarchy)
            {
                invalidColliders.Add(pair.Key);
            }
        }

        foreach (Collider itemCollider in invalidColliders)
            RemoveCollider(itemCollider);
    }

    private void RemoveActivator(GameObject activator)
    {
        if (!activators.TryGetValue(activator, out int colliderCount))
            return;

        if (colliderCount > 1)
            activators[activator] = colliderCount - 1;
        else
            activators.Remove(activator);

        RefreshState();
    }

    private void RefreshState()
    {
        bool shouldBePressed = activators.Count > 0;

        if (shouldBePressed == IsPressed)
            return;

        IsPressed = shouldBePressed;

        if (IsPressed)
            onPressed?.Invoke();
        else
            onReleased?.Invoke();
    }

    private void UpdateVisual()
    {
        if (plateVisual != null)
        {
            Vector3 targetPosition = releasedPosition + (IsPressed ? pressedOffset : Vector3.zero);
            plateVisual.localPosition = Vector3.MoveTowards(
                plateVisual.localPosition,
                targetPosition,
                movementSpeed * Time.deltaTime
            );
        }

        if (stateMaterial != null)
        {
            Color targetColor = IsPressed ? pressedColor : releasedColor;
            stateMaterial.color = Color.Lerp(
                stateMaterial.color,
                targetColor,
                colorSpeed * Time.deltaTime
            );
        }
    }

    protected virtual void OnDisable()
    {
        activeColliders.Clear();
        activators.Clear();

        if (IsPressed)
        {
            IsPressed = false;
            onReleased?.Invoke();
        }
    }
}
