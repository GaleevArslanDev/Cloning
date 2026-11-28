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

        GameSaveData save;

        try
        {
            if (!TryCaptureCurrentState(safeName, out save))
                return false;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Не удалось собрать данные сохранения: {exception.Message}");
            return false;
        }

        try
        {
            Directory.CreateDirectory(SavesFolder);
            string json = JsonUtility.ToJson(save, true);
            File.WriteAllText(GetFilePath(safeName), json);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is UnauthorizedAccessException ||
            exception is ArgumentException)
        {
            Debug.LogError($"Не удалось сохранить игру: {exception.Message}");
            return false;
        }
    }

    public static bool TryCaptureCurrentState(string slotName, out GameSaveData save)
    {
        save = null;
        CloneRecorder recorder = UnityEngine.Object.FindFirstObjectByType<CloneRecorder>();
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        FirstPersonCamera playerCamera = UnityEngine.Object.FindFirstObjectByType<FirstPersonCamera>();
        LevelManager levelManager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
        PlayerCarry playerCarry = player != null ? player.GetComponent<PlayerCarry>() : null;

        if (recorder == null || player == null)
        {
            Debug.LogWarning("Сохранение невозможно: не найдены игрок или CloneRecorder");
            return false;
        }

        save = new GameSaveData
        {
            Version = SaveDataValidator.CurrentVersion,
            SlotName = slotName,
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
            if (item == null)
                continue;

            string path = SceneSaveReference.GetPath(item);

            if (string.IsNullOrWhiteSpace(path))
                continue;

            save.Carryables.Add(new CarryableSaveData
            {
                Path = path,
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

            string path = SceneSaveReference.GetPath(behaviour);

            if (string.IsNullOrWhiteSpace(path))
                continue;

            string componentJson;

            try
            {
                componentJson = saveable.CaptureJsonState();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Не удалось получить состояние {behaviour.GetType().Name}: {exception.Message}",
                    behaviour
                );
                save = null;
                return false;
            }

            save.Components.Add(new ComponentSaveData
            {
                Path = path,
                ComponentType = behaviour.GetType().FullName,
                ComponentIndex = SceneSaveReference.GetComponentIndex(behaviour),
                Json = string.IsNullOrWhiteSpace(componentJson) ? "{}" : componentJson
            });
        }

        return SaveDataValidator.TryNormalize(save, slotName, out _);
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
            Debug.LogWarning(
                $"Сохранение '{slotName}' ссылается на недоступную сцену '{save.SceneName}'"
            );
            return false;
        }

        Time.timeScale = 1f;
        SaveLoadRunner runner = GetRunner();
        return runner.Load(save);
    }

    public static bool Delete(string slotName)
    {
        string safeName = GetSafeSlotName(slotName);

        if (string.IsNullOrWhiteSpace(safeName))
            return false;

        string path = GetFilePath(safeName);

        if (!File.Exists(path))
            return false;

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is UnauthorizedAccessException ||
            exception is ArgumentException)
        {
            Debug.LogError($"Не удалось удалить сохранение: {exception.Message}");
            return false;
        }
    }

    public static bool Exists(string slotName)
    {
        string safeName = GetSafeSlotName(slotName);
        return !string.IsNullOrWhiteSpace(safeName) &&
               File.Exists(GetFilePath(safeName));
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

        string[] files;

        try
        {
            files = Directory.GetFiles(SavesFolder, "*" + Extension);
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is UnauthorizedAccessException)
        {
            Debug.LogError($"Не удалось прочитать папку сохранений: {exception.Message}");
            return slots;
        }

        foreach (string file in files)
        {
            string fallbackName = Path.GetFileNameWithoutExtension(file);

            if (!TryReadFile(file, fallbackName, out GameSaveData save))
                continue;

            slots.Add(new SaveSlotInfo
            {
                Name = save.SlotName,
                SceneName = save.SceneName,
                SavedAt = save.SavedAt
            });
        }

        slots.Sort((left, right) => string.CompareOrdinal(right.SavedAt, left.SavedAt));
        return slots;
    }

    private static bool TryRead(string slotName, out GameSaveData save)
    {
        save = null;
        string safeName = GetSafeSlotName(slotName);

        if (string.IsNullOrWhiteSpace(safeName))
            return false;

        string path = GetFilePath(safeName);

        if (!File.Exists(path))
            return false;

        return TryReadFile(path, safeName, out save);
    }

    private static bool TryReadFile(
        string path,
        string fallbackSlotName,
        out GameSaveData save
    )
    {
        save = null;

        try
        {
            save = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));

            if (SaveDataValidator.TryNormalize(save, fallbackSlotName, out string error))
                return true;

            Debug.LogWarning($"Сохранение '{path}' пропущено: {error}");
            save = null;
            return false;
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is UnauthorizedAccessException ||
            exception is ArgumentException)
        {
            Debug.LogWarning($"Сохранение '{path}' повреждено: {exception.Message}");
            save = null;
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

        result = result.TrimEnd('.', ' ');

        if (result.Length > 80)
            result = result.Substring(0, 80).TrimEnd('.', ' ');

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
    private bool loading;

    public bool Load(GameSaveData save)
    {
        if (loading || save == null)
            return false;

        loading = true;
        pendingSave = save;
        StartCoroutine(LoadScene());
        return true;
    }

    private IEnumerator LoadScene()
    {
        if (pendingSave == null)
        {
            Destroy(gameObject);
            yield break;
        }

        LevelManager.PrepareForSaveLoad();
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(pendingSave.SceneName);

        if (loadOperation == null)
        {
            Debug.LogError($"Не удалось загрузить сцену '{pendingSave.SceneName}'");
            pendingSave = null;
            Destroy(gameObject);
            yield break;
        }

        yield return loadOperation;

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager != null)
        {
            float readyWait = 0f;

            while (!levelManager.IsReady && readyWait < 30f)
            {
                readyWait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!levelManager.IsReady)
            {
                Debug.LogWarning("Загрузка продолжена после таймаута подготовки уровня");
                PlayerControlLock control = FindFirstObjectByType<PlayerControlLock>();

                if (control != null)
                    control.SetControlEnabled(true);
            }
        }
        else
        {
            yield return null;
        }

        GameSaveData baseline = null;

        try
        {
            GameSaveSystem.TryCaptureCurrentState("Rollback", out baseline);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Не удалось подготовить откат загрузки: {exception.Message}");
        }

        if (baseline == null)
        {
            Debug.LogError("Загрузка отменена: текущее состояние нельзя безопасно восстановить");
        }
        else if (!TryRestore(pendingSave))
        {
            Debug.LogError("Загрузка отменена, восстанавливается исходное состояние сцены");

            if (!TryRestore(baseline))
                Debug.LogError("Не удалось полностью восстановить исходное состояние сцены");
        }

        pendingSave = null;
        Destroy(gameObject);
    }

    private bool TryRestore(GameSaveData save)
    {
        try
        {
            Restore(save);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Ошибка восстановления сохранения: {exception}");
            return false;
        }
    }

    private void Restore(GameSaveData save)
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        FirstPersonCamera playerCamera = FindFirstObjectByType<FirstPersonCamera>();
        CloneRecorder recorder = FindFirstObjectByType<CloneRecorder>();
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        if (player == null || recorder == null)
            throw new InvalidOperationException("Не найдены игрок или CloneRecorder");

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
