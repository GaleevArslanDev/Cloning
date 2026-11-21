using UnityEngine;

public class SaveGameController : MonoBehaviour
{
    [SerializeField] private string slotName = "Save 1";

    [ContextMenu("Save")]
    public void Save()
    {
        GameSaveSystem.Save(slotName);
    }

    [ContextMenu("Load")]
    public void Load()
    {
        GameSaveSystem.Load(slotName);
    }

    [ContextMenu("Delete")]
    public void Delete()
    {
        GameSaveSystem.Delete(slotName);
    }

    public void SetSlotName(string newSlotName)
    {
        slotName = newSlotName;
    }
}
