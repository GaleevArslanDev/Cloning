using UnityEngine;
using UnityEngine.UI;

public class SaveSlotView : MonoBehaviour
{
    [SerializeField] private Text saveName;
    [SerializeField] private Text sceneName;
    [SerializeField] private Text saveDate;

    private LoadSaveMenu owner;
    private string slotName;

    public void Show(LoadSaveMenu newOwner, SaveSlotInfo slot)
    {
        owner = newOwner;
        slotName = slot.Name;
        gameObject.SetActive(true);

        if (saveName != null)
            saveName.text = LoadSaveMenu.GetDisplayName(slot.Name);
        if (sceneName != null)
            sceneName.text = slot.SceneName;
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
