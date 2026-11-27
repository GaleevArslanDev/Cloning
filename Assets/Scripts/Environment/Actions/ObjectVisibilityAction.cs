using UnityEngine;

public class ObjectVisibilityAction : MonoBehaviour, IJsonSaveable
{
    private const int MaximumRequests = 1024;

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
        int objectCount = objectsToHide != null ? objectsToHide.Length : 0;

        if (objectCount == 0)
        {
            Debug.LogWarning("ObjectVisibilityAction: список объектов пуст", this);
            return;
        }

        if (activeRequests >= MaximumRequests)
        {
            Debug.LogWarning("ObjectVisibilityAction: превышен лимит запросов Hide", this);
            return;
        }

        activeRequests++;

        if (activeRequests > 1)
            return;

        initialStates = new bool[objectCount];

        for (int i = 0; i < objectCount; i++)
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

        EnsureInitialStates();

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
        if (string.IsNullOrWhiteSpace(json))
            return;

        SaveState state;

        try
        {
            state = JsonUtility.FromJson<SaveState>(json);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogWarning($"ObjectVisibilityAction: повреждён JSON: {exception.Message}", this);
            return;
        }

        if (state == null)
            return;

        int objectCount = objectsToHide != null ? objectsToHide.Length : 0;
        activeRequests = Mathf.Clamp(state.ActiveRequests, 0, MaximumRequests);
        initialStates = new bool[objectCount];

        for (int i = 0; i < objectCount; i++)
        {
            GameObject target = objectsToHide[i];

            if (target == null)
                continue;

            bool fallback = target.activeSelf;
            initialStates[i] = state.InitialStates != null && i < state.InitialStates.Length
                ? state.InitialStates[i]
                : fallback;
            bool current = state.CurrentStates != null && i < state.CurrentStates.Length
                ? state.CurrentStates[i]
                : activeRequests > 0 ? false : initialStates[i];
            target.SetActive(current);
        }
    }

    private void OnDisable()
    {
        if (activeRequests == 0)
            return;

        activeRequests = 1;
        Show();
    }

    private void EnsureInitialStates()
    {
        int objectCount = objectsToHide != null ? objectsToHide.Length : 0;

        if (initialStates != null && initialStates.Length == objectCount)
            return;

        bool[] repaired = new bool[objectCount];

        for (int i = 0; i < objectCount; i++)
        {
            repaired[i] = initialStates != null && i < initialStates.Length
                ? initialStates[i]
                : objectsToHide[i] != null && objectsToHide[i].activeSelf;
        }

        initialStates = repaired;
    }
}
