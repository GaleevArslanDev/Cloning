using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerClimber : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float ledgeClimbDuration = 0.4f;

    private CharacterController controller;
    private ClonePlayback mountedClone;
    private Transform originalParent;
    private bool canDismount;
    private Coroutine climbRoutine;

    public bool IsMounted => mountedClone != null;
    public bool IsClimbing => climbRoutine != null;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        controller = GetComponent<CharacterController>();
        originalParent = transform.parent;
    }

    public void Mount(ClonePlayback clone, Transform mountPoint)
    {
        if (clone == null || mountPoint == null)
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
        if (mountedClone == null && climbRoutine == null)
            return;

        if (climbRoutine != null)
        {
            StopCoroutine(climbRoutine);
            climbRoutine = null;
        }

        transform.SetParent(originalParent, true);
        mountedClone = null;
        canDismount = false;

        if (controller != null)
            controller.enabled = true;
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    public void ClimbTo(ClimbableLedge ledge)
    {
        if (!IsMounted || ledge == null)
            return;

        transform.SetParent(originalParent, true);
        mountedClone = null;
        canDismount = false;
        climbRoutine = StartCoroutine(ClimbToLedge(ledge.LandingPosition));
    }

    private IEnumerator ClimbToLedge(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float duration = Mathf.Max(0.01f, ledgeClimbDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, time / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        transform.position = targetPosition;
        climbRoutine = null;

        if (controller != null)
            controller.enabled = true;
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}
