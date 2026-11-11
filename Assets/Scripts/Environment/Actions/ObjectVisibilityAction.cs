using UnityEngine;

public class ObjectVisibilityAction : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToHide;

    private bool[] initialStates;
    private int activeRequests;

    public void Hide()
    {
        activeRequests++;

        if (activeRequests > 1)
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

    public void Show()
    {
        if (activeRequests == 0)
            return;

        activeRequests--;

        if (activeRequests > 0)
            return;

        for (int i = 0; i < objectsToHide.Length; i++)
        {
            if (objectsToHide[i] != null)
                objectsToHide[i].SetActive(initialStates[i]);
        }
    }

    private void OnDisable()
    {
        if (activeRequests == 0)
            return;

        activeRequests = 1;
        Show();
    }
}
