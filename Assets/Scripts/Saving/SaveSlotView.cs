using UnityEngine;
using UnityEngine.UI;

public class SaveSlotView : MonoBehaviour
{
    [SerializeField] private Text saveName;
    [SerializeField] private Text sceneName;
    [SerializeField] private Text saveDate;

    private LoadSaveMenu owner;
    private string slotName;

    private Button button;
    private ColorBlock defaultColors;

    private void Awake()
    {
        button = GetComponent<Button>();
        var background = GetComponent<Image>();
        button.targetGraphic = background;
        background.color = Color.white;
        defaultColors = button.colors;
    }

    public void UpdateSelection(string selectedSlot)
    {
        var colors = defaultColors;
        if (slotName == selectedSlot)
        {
            var selected = new Color(0.16f, 0.59f, 0.66f);
            colors.normalColor = colors.highlightedColor = colors.selectedColor = selected;
        }
        button.colors = colors;
    }

    public void Show(LoadSaveMenu newOwner, SaveSlotInfo slot)
    {
        owner = newOwner;
        slotName = slot.Name;
        gameObject.SetActive(true);

        if (saveName != null)
            saveName.text = LoadSaveMenu.GetDisplayName(slot.Name);
        if (sceneName != null)
            sceneName.text = GameSaveSystem.GetLevelNumber(slot.SceneName);
        if (saveDate != null)
            saveDate.text = LoadSaveMenu.FormatDate(slot.SavedAt);
    }

    public void Hide()
    {
        owner = null;
        slotName = string.Empty;
        gameObject.SetActive(false);
    }

    public void Select()
    {
        if (owner != null)
            owner.Select(slotName);
    }
}
