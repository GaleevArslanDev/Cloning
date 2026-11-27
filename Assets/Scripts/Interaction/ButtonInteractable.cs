using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : MonoBehaviour, IInteractable
{
    private AudioSource audioSource;
    private AudioClip pressedSound;
    private AudioClip releasedSound;
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReleased;
    [SerializeField, HideInInspector] private GameObject[] objectsToHide;

    private bool[] initialStates;
    private readonly HashSet<GameObject> interactors = new HashSet<GameObject>();
    private bool legacyActionActive;

    public bool CanInteract => enabled && gameObject.activeInHierarchy;
    public bool RecordInteraction => true;

    private void Awake()
    {
        audioSource = PlayerAudioController.GetOneShotSource();
        GameAudioClips audioClips = GameAudioClips.Instance;

        if (audioClips != null)
        {
            pressedSound = audioClips.ButtonPressed;
            releasedSound = audioClips.ButtonReleased;
        }
    }

    public void BeginInteraction(GameObject interactor)
    {
        if (interactor == null || !interactors.Add(interactor))
            return;

        if (interactors.Count > 1)
            return;

        legacyActionActive = !HasConfiguredEvents();
        PlaySound(pressedSound);
        onPressed?.Invoke();

        if (legacyActionActive)
            HideLegacyObjects();
    }

    public void Interact(GameObject interactor, float deltaTime)
    {
    }

    public void EndInteraction(GameObject interactor)
    {
        if (!interactors.Remove(interactor) || interactors.Count > 0)
            return;

        ReleaseButton();

        if (legacyActionActive)
        {
            ShowLegacyObjects();
            legacyActionActive = false;
        }
    }

    private void ReleaseButton()
    {
        PlaySound(releasedSound);
        onReleased?.Invoke();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private bool HasConfiguredEvents()
    {
        int pressedListeners = onPressed != null ? onPressed.GetPersistentEventCount() : 0;
        int releasedListeners = onReleased != null ? onReleased.GetPersistentEventCount() : 0;
        return pressedListeners > 0 || releasedListeners > 0;
    }

    private void HideLegacyObjects()
    {
        if (objectsToHide == null || objectsToHide.Length == 0)
            return;

        initialStates = new bool[objectsToHide.Length];

        int count = Mathf.Min(objectsToHide.Length, initialStates.Length);

        for (int i = 0; i < count; i++)
        {
            if (objectsToHide[i] == null)
                continue;

            initialStates[i] = objectsToHide[i].activeSelf;
            objectsToHide[i].SetActive(false);
        }
    }

    private void ShowLegacyObjects()
    {
        if (objectsToHide == null || initialStates == null)
            return;

        int count = Mathf.Min(objectsToHide.Length, initialStates.Length);

        for (int i = 0; i < count; i++)
        {
            if (objectsToHide[i] != null)
                objectsToHide[i].SetActive(initialStates[i]);
        }
    }

    private void OnDisable()
    {
        if (interactors.Count == 0)
            return;

        interactors.Clear();
        ReleaseButton();

        if (legacyActionActive)
        {
            ShowLegacyObjects();
            legacyActionActive = false;
        }
    }
}
