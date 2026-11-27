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
        int viewsCount = GetUsableViewCount();
        int startIndex = currentPage * Mathf.Max(1, viewsCount);
        int viewIndex = 0;

        if (slotViews != null)
        {
            foreach (SaveSlotView view in slotViews)
            {
                if (view == null)
                    continue;

                int slotIndex = startIndex + viewIndex;

                if (slotIndex < slots.Count)
                    view.Show(this, slots[slotIndex]);
                else
                    view.Hide();

                viewIndex++;
            }
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
        int viewsCount = GetUsableViewCount();

        if (viewsCount == 0 || slots.Count == 0)
            return 0;

        return Mathf.CeilToInt((float)slots.Count / viewsCount);
    }

    public void Select(string slotName)
    {
        selectedSlot = string.Empty;
        SetButtonsEnabled(false);

        if (string.IsNullOrWhiteSpace(slotName))
        {
            return;
        }

        foreach (SaveSlotInfo slot in GameSaveSystem.GetSlots())
        {
            if (slot.Name != slotName)
                continue;

            selectedSlot = slot.Name;
            SetSelectedInfo(
                GetDisplayName(slot.Name),
                GameSaveSystem.GetLevelNumber(slot.SceneName),
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
        if (string.IsNullOrWhiteSpace(slotName))
            return "Без названия";

        return slotName.StartsWith("Autosave", StringComparison.OrdinalIgnoreCase)
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

    private int GetUsableViewCount()
    {
        if (slotViews == null)
            return 0;

        int count = 0;

        foreach (SaveSlotView view in slotViews)
        {
            if (view != null)
                count++;
        }

        return count;
    }
}
