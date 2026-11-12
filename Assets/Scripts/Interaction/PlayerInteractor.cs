using System;
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
    [SerializeField] private Image mouseButtonImage;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color interactionColor = new Color(0.65f, 0.9f, 0.65f);
    [SerializeField] private PlayerCarry playerCarry;
    [SerializeField] private PlayerClimber playerClimber;

    private IInteractable currentInteractable;
    private Vector3 mouseButtonScale;

    public event Action<IInteractable> InteractionStarted;
    public event Action<IInteractable> InteractionEnded;

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = GetComponentInChildren<Camera>();
        if (playerCarry == null)
            playerCarry = GetComponent<PlayerCarry>();
        if (playerClimber == null)
            playerClimber = GetComponent<PlayerClimber>();

        if (interactionPanelImage == null && interactionPanel != null)
            interactionPanelImage = interactionPanel.GetComponentInChildren<Image>(true);

        if (mouseButtonImage != null)
            mouseButtonScale = mouseButtonImage.rectTransform.localScale;

        SetPanelColor(false);
        SetPanelVisible(false);
    }

    private void Update()
    {
        if (playerClimber != null && playerClimber.IsMounted)
        {
            StopInteraction();
            ClimbableLedge ledge = FindClimbableLedge();
            bool climbPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

            SetPanelVisible(ledge != null);
            SetPanelColor(climbPressed && ledge != null);
            SetRightMouseHint(ledge != null);

            if (climbPressed && ledge != null)
                playerClimber.ClimbTo(ledge);

            playerClimber.HandleDismountInput();
            return;
        }

        if (playerClimber != null && playerClimber.IsClimbing)
        {
            StopInteraction();
            SetPanelVisible(false);
            SetPanelColor(false);
            SetRightMouseHint(false);
            return;
        }

        if (playerCarry != null && playerCarry.IsBusy)
        {
            StopInteraction();
            SetPanelVisible(false);
            SetPanelColor(false);
            SetRightMouseHint(false);
            return;
        }

        SetRightMouseHint(false);
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

        if (currentInteractable is MonoBehaviour currentTarget && currentTarget != null)
            currentInteractable.Interact(gameObject, Time.deltaTime);
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

    private ClimbableLedge FindClimbableLedge()
    {
        if (interactionCamera == null)
            return null;

        Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers))
            return null;

        return hit.collider.GetComponentInParent<ClimbableLedge>();
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

    private void SetRightMouseHint(bool isRightMouse)
    {
        if (mouseButtonImage == null)
            return;

        Vector3 scale = mouseButtonScale;

        if (isRightMouse)
            scale.x = -scale.x;

        mouseButtonImage.rectTransform.localScale = scale;
    }

    private void StartInteraction(IInteractable interactable)
    {
        if (interactable == null)
            return;

        currentInteractable = interactable;
        InteractionStarted?.Invoke(currentInteractable);
        currentInteractable.BeginInteraction(gameObject);
    }

    private void StopInteraction()
    {
        if (currentInteractable == null)
            return;

        IInteractable interactable = currentInteractable;
        currentInteractable = null;

        if (interactable is MonoBehaviour target && target != null)
        {
            interactable.EndInteraction(gameObject);
            InteractionEnded?.Invoke(interactable);
        }
    }

    private void OnDisable()
    {
        StopInteraction();
        SetPanelVisible(false);
        SetRightMouseHint(false);
    }
}
