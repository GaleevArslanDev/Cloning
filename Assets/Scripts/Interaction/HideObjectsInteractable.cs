using UnityEngine;

public class HideObjectsInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject[] objectsToHide;

    private bool[] initialStates;
    private bool isInteracting;

    public bool CanInteract => enabled && gameObject.activeInHierarchy;

    public void BeginInteraction()
    {
        if (isInteracting)
            return;

        initialStates = new bool[objectsToHide.Length];

        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] == null)
                continue;

            initialStates[i] = objectsToHide[i].activeSelf;
            objectsToHide[i].SetActive(false);
        }

        isInteracting = true;
    }

    public void Interact(float deltaTime)
    {
    }

    public void EndInteraction()
    {
        if (!isInteracting)
            return;

        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] != null)
                objectsToHide[i].SetActive(initialStates[i]);
        }

        isInteracting = false;
    }

    private void OnDisable()
    {
        EndInteraction();
    }
}
