using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Standard uGUI elements, matching MainMenu's sizes, font and colours.
public static class PauseMenuView
{
    public static GameObject Create(Transform parent, UnityAction resume, UnityAction restart, UnityAction exit)
    {
        var root = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        root.layer = 5;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var background = Rect("DimBackground", root.transform, Vector2.zero, Vector2.zero);
        Stretch(background);
        background.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.025f, 0.04f, 0.45f);
        var panel = Rect("PausePanel", root.transform, new Vector2(520, 620), Vector2.zero);
        var accent = Rect("Accent", panel, new Vector2(203.3f, 8.7f), new Vector2(0, 210));
        accent.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.8f, 0.87f);
        Label("Title", panel, "Intaraction", 72, new Vector2(520, 100), new Vector2(0, 165), Color.white);
        Label("Subtitle", panel, "ПАУЗА", 20, new Vector2(520, 40), new Vector2(0, 95), new Color(0.45087218f, 0.8007721f, 0.9622642f));
        var first = Button(panel, "ResumeButton", "Продолжить", 10, new Color(0.10f, 0.48f, 0.55f), resume);
        var second = Button(panel, "RestartButton", "Перезагрузить", -90, new Color(0.12f, 0.17f, 0.22f), restart);
        var third = Button(panel, "ExitButton", "Выйти", -190, new Color(0.12f, 0.17f, 0.22f), exit);
        var buttons = new[] { first, second, third };
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i + 2) % 3], selectOnDown = buttons[(i + 1) % 3] };

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(parent, false);
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        return root;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Text Label(string name, Transform parent, string caption, int size, Vector2 dimensions, Vector2 position, Color color)
    {
        var label = Rect(name, parent, dimensions, position).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = caption;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(Transform parent, string name, string caption, float y, Color color, UnityAction action)
    {
        var rect = Rect(name, parent, new Vector2(440, 76), new Vector2(0, y));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.selectedColor = new Color(1.12f, 1.12f, 1.12f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.onClick.AddListener(action);
        Stretch(Label("Label", rect, caption, 28, Vector2.zero, Vector2.zero, Color.white).rectTransform);
        return button;
    }
}
