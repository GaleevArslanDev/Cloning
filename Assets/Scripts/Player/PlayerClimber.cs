using UnityEngine;

public class PlayerClimber : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    private CharacterController controller;
    private ClonePlayback mountedClone;
    private Transform originalParent;

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

    public void Dismount()
    {
        if (mountedClone == null)
            return;

        transform.SetParent(originalParent, true);
        mountedClone = null;

        if (controller != null)
            controller.enabled = true;
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}
