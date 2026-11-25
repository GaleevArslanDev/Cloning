using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerClimber : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float ledgeClimbDuration = 0.4f;
    [SerializeField] private float mountJumpDuration = 0.45f;
    [SerializeField] private float mountJumpHeight = 0.2f;
    [SerializeField] private AudioSource climbAudioSource;
    [SerializeField] private AudioClip climbSound;

    private CharacterController controller;
    private ClonePlayback mountedClone;
    private Transform originalParent;
    private bool canDismount;
    private Coroutine climbRoutine;
    private float jumpAnimationTime;

    public bool IsMounted => mountedClone != null;
    public bool IsClimbing => climbRoutine != null;
    public bool IsJumping => IsClimbing || jumpAnimationTime > 0f;
    public bool CanStartClimb => !IsClimbing &&
                                 (IsMounted ||
                                  controller != null &&
                                  controller.enabled &&
                                  controller.isGrounded);

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        controller = GetComponent<CharacterController>();
        originalParent = transform.parent;
    }

    private void Update()
    {
        if (jumpAnimationTime > 0f)
            jumpAnimationTime -= Time.deltaTime;
    }

    public void Mount(ClonePlayback clone, Transform mountPoint)
    {
        if (clone == null || mountPoint == null)
            return;

        Dismount();
        mountedClone = clone;
        canDismount = false;
        jumpAnimationTime = mountJumpDuration;
        PlayClimbSound();

        if (playerMovement != null)
            playerMovement.enabled = false;
        if (controller != null)
            controller.enabled = false;

        transform.SetParent(mountPoint, true);
        climbRoutine = StartCoroutine(JumpToMountPoint());
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
        if (ledge == null || !CanStartClimb)
            return;

        if (playerMovement != null)
            playerMovement.enabled = false;
        if (controller != null)
            controller.enabled = false;

        transform.SetParent(originalParent, true);
        mountedClone = null;
        canDismount = false;
        jumpAnimationTime = Mathf.Max(jumpAnimationTime, ledgeClimbDuration);
        PlayClimbSound();
        climbRoutine = StartCoroutine(ClimbToLedge(ledge.LandingPosition));
    }

    private void PlayClimbSound()
    {
        if (climbAudioSource != null && climbSound != null)
            climbAudioSource.PlayOneShot(climbSound);
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

    private IEnumerator JumpToMountPoint()
    {
        Vector3 startPosition = transform.localPosition;
        float duration = Mathf.Max(0.01f, mountJumpDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            float jumpOffset = Mathf.Sin(progress * Mathf.PI) * mountJumpHeight;

            transform.localPosition = Vector3.Lerp(
                startPosition,
                Vector3.zero,
                smoothProgress
            ) + Vector3.up * jumpOffset;

            yield return null;
        }

        transform.localPosition = Vector3.zero;
        climbRoutine = null;
    }
}
