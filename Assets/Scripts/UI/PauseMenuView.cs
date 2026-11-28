using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Older test scenes with a player can use the same prefab as the authored levels.
public static class PauseMenuView
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= InstallForTestScene;
        SceneManager.sceneLoaded += InstallForTestScene;
    }

    private static void InstallForTestScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") return;

        bool hasPlayer = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<PauseMenuController>(true) != null) return;
            if (!hasPlayer) hasPlayer = root.GetComponentInChildren<PlayerMovement>(true) != null;
        }
        if (!hasPlayer) return;

        var prefab = Resources.Load<GameObject>("PauseMenu");
        var instance = Object.Instantiate(prefab);
        SceneManager.MoveGameObjectToScene(instance, scene);
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(instance.transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
