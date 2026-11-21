using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadSaveMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private SaveSlotView[] slotViews;
    [SerializeField] private Text selectedName;
    [SerializeField] private Text selectedScene;
    [SerializeField] private Text selectedDate;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Text pageText;

    private List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
    private string selectedSlot;
    private int currentPage;

    private void Awake()
    {
        Close();
    }

    public void Open()
    {
        if (menuRoot != null)
            menuRoot.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    public void Refresh()
    {
        slots = GameSaveSystem.GetSlots();
        selectedSlot = string.Empty;
        int pageCount = GetPageCount();
        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, pageCount - 1));
        FillPage();
        SetSelectedInfo(string.Empty, string.Empty, string.Empty);
        SetButtonsEnabled(false);
    }

    public void NextPage()
    {
        if (currentPage >= GetPageCount() - 1)
            return;

        currentPage++;
        FillPage();
    }

    public void PreviousPage()
    {
        if (currentPage <= 0)
            return;

        currentPage--;
        FillPage();
    }

    private void FillPage()
    {
        int viewsCount = slotViews != null ? slotViews.Length : 0;
        int startIndex = currentPage * Mathf.Max(1, viewsCount);

        for (int i = 0; i < viewsCount; i++)
        {
            int slotIndex = startIndex + i;

            if (slotIndex < slots.Count)
                slotViews[i].Show(this, slots[slotIndex]);
            else
                slotViews[i].Hide();
        }

        int pageCount = GetPageCount();

        if (pageText != null)
            pageText.text = pageCount > 0 ? $"{currentPage + 1} / {pageCount}" : "0 / 0";
        if (previousPageButton != null)
            previousPageButton.interactable = currentPage > 0;
        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < pageCount - 1;
    }

    private int GetPageCount()
    {
        int viewsCount = slotViews != null ? slotViews.Length : 0;

        if (viewsCount == 0 || slots.Count == 0)
            return 0;

        return Mathf.CeilToInt((float)slots.Count / viewsCount);
    }

    public void Select(string slotName)
    {
        foreach (SaveSlotInfo slot in GameSaveSystem.GetSlots())
        {
            if (slot.Name != slotName)
                continue;

            selectedSlot = slot.Name;
            SetSelectedInfo(
                GetDisplayName(slot.Name),
                slot.SceneName,
                FormatDate(slot.SavedAt)
            );
            SetButtonsEnabled(true);
            return;
        }
    }

    public void LoadSelected()
    {
        if (string.IsNullOrWhiteSpace(selectedSlot))
            return;

        Time.timeScale = 1f;
        GameSaveSystem.Load(selectedSlot);
    }

    public void DeleteSelected()
    {
        if (string.IsNullOrWhiteSpace(selectedSlot))
            return;

        GameSaveSystem.Delete(selectedSlot);
        Refresh();
    }

    public static string GetDisplayName(string slotName)
    {
        return string.Equals(slotName, "Autosave", StringComparison.OrdinalIgnoreCase)
            ? "Автосохранение"
            : slotName;
    }

    public static string FormatDate(string value)
    {
        if (DateTime.TryParse(value, out DateTime date))
            return date.ToString("dd.MM.yyyy  HH:mm");

        return value;
    }

    private void SetSelectedInfo(string name, string scene, string date)
    {
        if (selectedName != null)
            selectedName.text = name;
        if (selectedScene != null)
            selectedScene.text = scene;
        if (selectedDate != null)
            selectedDate.text = date;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (loadButton != null)
            loadButton.interactable = enabled;
        if (deleteButton != null)
            deleteButton.interactable = enabled;
    }
}
