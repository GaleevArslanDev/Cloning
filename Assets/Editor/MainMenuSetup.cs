using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSetup
{
    [MenuItem("Tools/ECHO/Create Main Menu")]
    public static void Create()
    {
        const string path = "Assets/Scenes/MainMenu.unity";
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != path) throw new System.InvalidOperationException("Open MainMenu first.");
        if (Object.FindFirstObjectByType<MainMenuController>() != null)
        {
            Debug.Log("MainMenu already configured.");
            return;
        }
        var controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
        var canvas = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.layer = 5;
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var bg = Rect("Background", canvas.transform, Vector2.zero, Vector2.zero);
        Stretch(bg);
        bg.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1);
        var panel = Rect("Menu", canvas.transform, new Vector2(520, 620), Vector2.zero);
        var accent = Rect("Accent", panel, new Vector2(64, 4), new Vector2(0, 240));
        accent.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.8f, 0.87f);
        Label("Title", panel, "ECHO", 72, new Vector2(520, 100), new Vector2(0, 165), Color.white);
        Label("Subtitle", panel, "ГЛАВНОЕ МЕНЮ", 20, new Vector2(520, 40), new Vector2(0, 95), new Color(0.55f, 0.65f, 0.7f));
        var resume = MakeButton("ContinueButton", panel, "Продолжить", 10, new Color(0.18f, 0.20f, 0.23f));
        resume.interactable = false;
        resume.GetComponentInChildren<Text>().color = new Color(0.44f, 0.47f, 0.51f);
        var start = MakeButton("NewGameButton", panel, "Новая игра", -90, new Color(0.10f, 0.48f, 0.55f));
        var quit = MakeButton("QuitButton", panel, "Выйти", -190, new Color(0.12f, 0.17f, 0.22f));
        UnityEventTools.AddPersistentListener(start.onClick, controller.NewGame);
        UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);
        var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = quit, selectOnDown = quit };
        start.navigation = nav;
        nav.selectOnUp = start; nav.selectOnDown = start; quit.navigation = nav;
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        es.firstSelectedGameObject = start.gameObject;
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        const string levelPath = "Assets/Scenes/Level1.unity";
        var first = scenes.FirstOrDefault(s => s.path == levelPath);
        if (first != null) first.enabled = true;
        else scenes.Add(new EditorBuildSettingsScene(levelPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = canvas;
        Debug.Log("MainMenu configured: Continue disabled, New Game -> Level1, Quit -> exit.");
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size; r.anchoredPosition = position;
        return r;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static Text Label(string name, Transform parent, string text, int size, Vector2 dimensions, Vector2 position, Color color)
    {
        var label = Rect(name, parent, dimensions, position).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text; label.fontSize = size; label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Button MakeButton(string name, Transform parent, string caption, float y, Color color)
    {
        var r = Rect(name, parent, new Vector2(440, 76), new Vector2(0, y));
        var image = r.gameObject.AddComponent<Image>(); image.color = color;
        var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.selectedColor = new Color(1.12f, 1.12f, 1.12f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.disabledColor = Color.white; colors.fadeDuration = 0.12f;
        button.colors = colors;
        var label = Label("Label", r, caption, 28, Vector2.zero, Vector2.zero, Color.white);
        Stretch(label.rectTransform);
        return button;
    }
}
