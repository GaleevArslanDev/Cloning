using System;
using UnityEngine;

public class TwoPointsUpperInteractor : MonoBehaviour
{
    private Transform viewPoint;
    private float interactionDistance;
    private LayerMask interactionLayers;
    private Action<Vector3> climbAction;
    private IInteractable currentInteractable;
    private Vector3 climbUpDirection;
    private PlayerCarry playerCarry;
    private bool wasLeftHeld;

    public void Configure(
        Transform newViewPoint,
        float newInteractionDistance,
        LayerMask newInteractionLayers,
        Vector3 newClimbUpDirection,
        Action<Vector3> newClimbAction,
        PlayerCarry newPlayerCarry
    )
    {
        viewPoint = newViewPoint;
        interactionDistance = newInteractionDistance;
        interactionLayers = newInteractionLayers;
        climbUpDirection = newClimbUpDirection;
        climbAction = newClimbAction;
        playerCarry = newPlayerCarry;
    }

    public void ProcessInput(bool leftHeld, bool rightPressed, float scroll, float deltaTime)
    {
        bool leftPressed = leftHeld && !wasLeftHeld;
        wasLeftHeld = leftHeld;

        if (playerCarry != null && playerCarry.IsBusy)
        {
            if (leftPressed)
                playerCarry.RequestDrop();

            playerCarry.RequestRotation(scroll);

            StopInteraction();
            return;
        }

        if (rightPressed)
            TryClimb();

        if (!leftHeld)
        {
            StopInteraction();
            return;
        }

        IInteractable interactable = FindInteractable();

        if (interactable != currentInteractable)
        {
            StopInteraction();
            StartInteraction(interactable);
        }

        if (currentInteractable is MonoBehaviour target && target != null)
            currentInteractable.Interact(gameObject, deltaTime);
    }

    private IInteractable FindInteractable()
    {
        if (!TryGetHit(out RaycastHit hit))
            return null;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null || !interactable.CanInteract)
            return null;

        return interactable;
    }

    private void TryClimb()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        ClimbableLedge ledge = hit.collider.GetComponentInParent<ClimbableLedge>();

        if (ledge != null)
            climbAction?.Invoke(ledge.GetLandingPosition(climbUpDirection));
    }

    private bool TryGetHit(out RaycastHit hit)
    {
        hit = new RaycastHit();

        if (viewPoint == null)
            return false;

        Ray ray = new Ray(viewPoint.position, viewPoint.forward);
        return Physics.Raycast(ray, out hit, interactionDistance, interactionLayers);
    }

    private void StartInteraction(IInteractable interactable)
    {
        if (interactable == null)
            return;

        currentInteractable = interactable;
        currentInteractable.BeginInteraction(gameObject);
    }

    private void StopInteraction()
    {
        if (currentInteractable == null)
            return;

        IInteractable interactable = currentInteractable;
        currentInteractable = null;

        if (interactable is MonoBehaviour target && target != null)
            interactable.EndInteraction(gameObject);
    }

    private void OnDisable()
    {
        wasLeftHeld = false;
        StopInteraction();
    }
}
