using System.Collections.Generic;
using UnityEngine;

public class HideObjectsInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject[] objectsToHide;

    private bool[] initialStates;
    private readonly HashSet<GameObject> interactors = new HashSet<GameObject>();

    public bool CanInteract => enabled && gameObject.activeInHierarchy;
    public bool RecordInteraction => true;

    public void BeginInteraction(GameObject interactor)
    {
        if (interactor == null || !interactors.Add(interactor))
            return;

        if (interactors.Count > 1)
            return;

        initialStates = new bool[objectsToHide.Length];

        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] == null)
                continue;

            initialStates[i] = objectsToHide[i].activeSelf;
            objectsToHide[i].SetActive(false);
        }

    }

    public void Interact(GameObject interactor, float deltaTime)
    {
    }

    public void EndInteraction(GameObject interactor)
    {
        if (!interactors.Remove(interactor) || interactors.Count > 0)
            return;

        for (int i = 0; i < objectsToHide.Length; i++)
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

        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] != null)
                objectsToHide[i].SetActive(initialStates[i]);
        }
    }
}
