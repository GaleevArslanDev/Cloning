using UnityEngine;

public class ObjectVisibilityAction : MonoBehaviour, IJsonSaveable
{
    [SerializeField] private GameObject[] objectsToHide;

    private bool[] initialStates;
    private int activeRequests;

    [System.Serializable]
    private class SaveState
    {
        public int ActiveRequests;
        public bool[] InitialStates;
        public bool[] CurrentStates;
    }

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

    public string CaptureJsonState()
    {
        int objectCount = objectsToHide != null ? objectsToHide.Length : 0;
        bool[] currentStates = new bool[objectCount];

        for (int i = 0; i < objectCount; i++)
            currentStates[i] = objectsToHide[i] != null && objectsToHide[i].activeSelf;

        return JsonUtility.ToJson(new SaveState
        {
            ActiveRequests = activeRequests,
            InitialStates = initialStates,
            CurrentStates = currentStates
        });
    }

    public void RestoreJsonState(string json)
    {
        SaveState state = JsonUtility.FromJson<SaveState>(json);

        if (state == null)
            return;

        activeRequests = Mathf.Max(0, state.ActiveRequests);
        initialStates = state.InitialStates;

        if (state.CurrentStates == null)
            return;

        int objectCount = objectsToHide != null ? objectsToHide.Length : 0;
        int count = Mathf.Min(objectCount, state.CurrentStates.Length);

        for (int i = 0; i < count; i++)
        {
            if (objectsToHide[i] != null)
                objectsToHide[i].SetActive(state.CurrentStates[i]);
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
