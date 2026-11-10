using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Image interactionPanelImage;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color interactionColor = new Color(0.65f, 0.9f, 0.65f);

    private IInteractable currentInteractable;

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = GetComponentInChildren<Camera>();

        if (interactionPanelImage == null && interactionPanel != null)
            interactionPanelImage = interactionPanel.GetComponentInChildren<Image>(true);

        SetPanelColor(false);
        SetPanelVisible(false);
    }

    private void Update()
    {
        IInteractable interactable = FindInteractable();
        bool interactionHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;

        SetPanelVisible(interactable != null);
        SetPanelColor(interactionHeld && interactable != null);

        if (!interactionHeld)
        {
            StopInteraction();
            return;
        }

        if (interactable != currentInteractable)
        {
            StopInteraction();
            StartInteraction(interactable);
        }

        if (currentInteractable != null)
            currentInteractable.Interact(Time.deltaTime);
    }

    private IInteractable FindInteractable()
    {
        if (interactionCamera == null)
            return null;

        Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers))
            return null;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null || !interactable.CanInteract)
            return null;

        return interactable;
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (interactionPanel != null && interactionPanel.activeSelf != isVisible)
            interactionPanel.SetActive(isVisible);
    }

    private void SetPanelColor(bool isInteracting)
    {
        if (interactionPanelImage != null)
            interactionPanelImage.color = isInteracting ? interactionColor : hoverColor;
    }

    private void StartInteraction(IInteractable interactable)
    {
        if (interactable == null)
            return;

        currentInteractable = interactable;
        currentInteractable.BeginInteraction();
    }

    private void StopInteraction()
    {
        if (currentInteractable == null)
            return;

        currentInteractable.EndInteraction();
        currentInteractable = null;
    }

    private void OnDisable()
    {
        StopInteraction();
        SetPanelVisible(false);
    }
}
