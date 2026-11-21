using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSaveSystem
{
    private const string Extension = ".json";

    public static string SavesFolder => Path.Combine(
        Application.persistentDataPath,
        "Saves"
    );

    public static bool Save(string slotName)
    {
        string safeName = GetSafeSlotName(slotName);

        if (string.IsNullOrWhiteSpace(safeName))
            return false;

        CloneRecorder recorder = UnityEngine.Object.FindFirstObjectByType<CloneRecorder>();
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        FirstPersonCamera playerCamera = UnityEngine.Object.FindFirstObjectByType<FirstPersonCamera>();
        LevelManager levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
        PlayerCarry playerCarry = player != null ? player.GetComponent<PlayerCarry>() : null;

        if (recorder == null || player == null)
            return false;

        GameSaveData save = new GameSaveData
        {
            SlotName = safeName,
            SavedAt = DateTime.Now.ToString("O"),
            SceneName = SceneManager.GetActiveScene().name,
            LevelConditionCompleted = levelManager != null &&
                levelManager.LevelConditionCompleted,
            Recorder = recorder.CaptureSaveData()
        };

        save.Player.Position = player.transform.position;
        save.Player.Rotation = player.transform.rotation;
        save.Player.CameraPitch = playerCamera != null ? playerCamera.Pitch : 0f;
        save.Player.HeldItemPath = playerCarry != null
            ? SceneSaveReference.GetPath(playerCarry.HeldItem)
            : string.Empty;

        CarryableItem[] items = UnityEngine.Object.FindObjectsByType<CarryableItem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (CarryableItem item in items)
        {
            save.Carryables.Add(new CarryableSaveData
            {
                Path = SceneSaveReference.GetPath(item),
                Position = item.transform.position,
                Rotation = item.transform.rotation,
                Active = item.gameObject.activeSelf,
                IsKinematic = item.IsKinematic,
                UseGravity = item.UsesGravity
            });
        }

        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (!(behaviour is IJsonSaveable saveable))
                continue;

            save.Components.Add(new ComponentSaveData
            {
                Path = SceneSaveReference.GetPath(behaviour),
                ComponentType = behaviour.GetType().FullName,
                ComponentIndex = SceneSaveReference.GetComponentIndex(behaviour),
                Json = saveable.CaptureJsonState()
            });
        }

        try
        {
            Directory.CreateDirectory(SavesFolder);
            string json = JsonUtility.ToJson(save, true);
            File.WriteAllText(GetFilePath(safeName), json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SaveNew(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName) || Exists(slotName))
            return false;

        return Save(slotName);
    }

    public static bool Load(string slotName)
    {
        if (!TryRead(slotName, out GameSaveData save))
            return false;

        if (string.IsNullOrWhiteSpace(save.SceneName) ||
            !Application.CanStreamedLevelBeLoaded(save.SceneName))
        {
            return false;
        }

        Time.timeScale = 1f;
        SaveLoadRunner runner = GetRunner();
        runner.Load(save);
        return true;
    }

    public static bool Delete(string slotName)
    {
        string path = GetFilePath(GetSafeSlotName(slotName));

        if (!File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }

    public static bool Exists(string slotName)
    {
        return File.Exists(GetFilePath(GetSafeSlotName(slotName)));
    }

    public static string GetLevelNumber(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "-";

        string number = string.Empty;

        foreach (char character in sceneName)
        {
            if (char.IsDigit(character))
            {
                number += character;
                continue;
            }

            if (number.Length > 0)
                break;
        }

        return number.Length > 0 ? number : sceneName;
    }

    public static List<SaveSlotInfo> GetSlots()
    {
        List<SaveSlotInfo> slots = new List<SaveSlotInfo>();

        if (!Directory.Exists(SavesFolder))
            return slots;

        foreach (string file in Directory.GetFiles(SavesFolder, "*" + Extension))
        {
            try
            {
                GameSaveData save = JsonUtility.FromJson<GameSaveData>(
                    File.ReadAllText(file)
                );

                if (save == null)
                    continue;

                slots.Add(new SaveSlotInfo
                {
                    Name = save.SlotName,
                    SceneName = save.SceneName,
                    SavedAt = save.SavedAt
                });
            }
            catch
            {
            }
        }

        slots.Sort((left, right) => string.CompareOrdinal(right.SavedAt, left.SavedAt));
        return slots;
    }

    private static bool TryRead(string slotName, out GameSaveData save)
    {
        save = null;
        string path = GetFilePath(GetSafeSlotName(slotName));

        if (!File.Exists(path))
            return false;

        try
        {
            save = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
            return save != null;
        }
        catch
        {
            return false;
        }
    }

    private static string GetSafeSlotName(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName))
            return string.Empty;

        string result = slotName.Trim();

        foreach (char character in Path.GetInvalidFileNameChars())
            result = result.Replace(character, '_');

        return result;
    }

    private static string GetFilePath(string slotName)
    {
        return Path.Combine(SavesFolder, slotName + Extension);
    }

    private static SaveLoadRunner GetRunner()
    {
        SaveLoadRunner runner = UnityEngine.Object.FindFirstObjectByType<SaveLoadRunner>();

        if (runner != null)
            return runner;

        GameObject gameObject = new GameObject("Save Load Runner");
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        return gameObject.AddComponent<SaveLoadRunner>();
    }
}

public class SaveLoadRunner : MonoBehaviour
{
    private GameSaveData pendingSave;

    public void Load(GameSaveData save)
    {
        pendingSave = save;
        StartCoroutine(LoadScene());
    }

    private IEnumerator LoadScene()
    {
        LevelManager.PrepareForSaveLoad();
        yield return SceneManager.LoadSceneAsync(pendingSave.SceneName);

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager != null)
        {
            while (!levelManager.IsReady)
                yield return null;
        }
        else
        {
            yield return null;
        }

        Restore(pendingSave);
        pendingSave = null;
        Destroy(gameObject);
    }

    private void Restore(GameSaveData save)
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        FirstPersonCamera playerCamera = FindFirstObjectByType<FirstPersonCamera>();
        CloneRecorder recorder = FindFirstObjectByType<CloneRecorder>();
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        if (player == null || recorder == null)
            return;

        PlayerCarry playerCarry = player.GetComponent<PlayerCarry>();

        if (playerCarry != null)
            playerCarry.CancelCarry();

        foreach (CarryableSaveData itemSave in save.Carryables)
        {
            CarryableItem item = SceneSaveReference.FindComponent<CarryableItem>(
                itemSave.Path
            );

            if (item == null)
                continue;

            item.gameObject.SetActive(itemSave.Active);
            item.RestoreSavedState(
                itemSave.Position,
                itemSave.Rotation,
                itemSave.IsKinematic,
                itemSave.UseGravity
            );
        }

        player.Teleport(save.Player.Position, save.Player.Rotation);

        if (playerCamera != null)
            playerCamera.SetLook(save.Player.Rotation, save.Player.CameraPitch);

        recorder.RestoreSaveData(save.Recorder);

        if (levelManager != null)
            levelManager.RestoreLevelCondition(save.LevelConditionCompleted);

        if (playerCarry != null && !string.IsNullOrWhiteSpace(save.Player.HeldItemPath))
        {
            CarryableItem heldItem = SceneSaveReference.FindComponent<CarryableItem>(
                save.Player.HeldItemPath
            );
            playerCarry.RestoreHeldItem(heldItem);
        }

        foreach (ComponentSaveData componentSave in save.Components)
        {
            MonoBehaviour behaviour = SceneSaveReference.FindBehaviour(
                componentSave.Path,
                componentSave.ComponentType,
                componentSave.ComponentIndex
            );

            if (behaviour is IJsonSaveable saveable)
                saveable.RestoreJsonState(componentSave.Json);
        }

        Physics.SyncTransforms();
    }
}
