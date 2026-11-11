using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClimber : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    private CharacterController controller;
    private ClonePlayback mountedClone;
    private Transform originalParent;
    private bool canDismount;

    public bool IsMounted => mountedClone != null;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        controller = GetComponent<CharacterController>();
        originalParent = transform.parent;
    }

    public void Mount(ClonePlayback clone, Transform mountPoint)
    {
        if (clone == null || mountPoint == null || !clone.IsStandingStill)
            return;

        Dismount();
        mountedClone = clone;
        canDismount = false;

        if (playerMovement != null)
            playerMovement.enabled = false;
        if (controller != null)
            controller.enabled = false;

        transform.SetParent(mountPoint);
        transform.localPosition = Vector3.zero;
    }

    public void DismountFrom(ClonePlayback clone)
    {
        if (mountedClone == clone)
            Dismount();
    }

    public void HandleDismountInput()
    {
        if (!IsMounted || Mouse.current == null)
            return;

        if (!canDismount)
        {
            if (!Mouse.current.leftButton.isPressed)
                canDismount = true;

            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
            Dismount();
    }

    public void Dismount()
    {
        if (mountedClone == null)
            return;

        transform.SetParent(originalParent, true);
        mountedClone = null;
        canDismount = false;

        if (controller != null)
            controller.enabled = true;
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}
