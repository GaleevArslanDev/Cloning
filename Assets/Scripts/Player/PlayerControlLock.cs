using UnityEngine;

public class PlayerControlLock : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PlayerCarry playerCarry;
    [SerializeField] private FirstPersonCamera playerCamera;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (playerInteractor == null)
            playerInteractor = GetComponent<PlayerInteractor>();
        if (playerCarry == null)
            playerCarry = GetComponent<PlayerCarry>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<FirstPersonCamera>();
    }

    public void SetControlEnabled(bool isEnabled)
    {
        if (playerMovement != null)
            playerMovement.enabled = isEnabled;
        if (playerInteractor != null)
            playerInteractor.enabled = isEnabled;
        if (playerCarry != null)
            playerCarry.enabled = isEnabled;
        if (playerCamera != null)
            playerCamera.enabled = isEnabled;
    }
}
