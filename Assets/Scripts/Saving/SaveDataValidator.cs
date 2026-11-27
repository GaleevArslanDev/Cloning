using System;
using System.Collections.Generic;
using UnityEngine;

public static class SaveDataValidator
{
    public const int CurrentVersion = 1;

    public static bool TryNormalize(GameSaveData save, string fallbackSlotName, out string error)
    {
        error = string.Empty;

        if (save == null)
        {
            error = "Файл сохранения не содержит данных";
            return false;
        }

        if (save.Version == 0)
            save.Version = CurrentVersion;

        if (save.Version != CurrentVersion)
        {
            error = $"Неподдерживаемая версия сохранения: {save.Version}";
            return false;
        }

        save.SlotName = string.IsNullOrWhiteSpace(save.SlotName)
            ? fallbackSlotName
            : save.SlotName.Trim();

        if (string.IsNullOrWhiteSpace(save.SlotName))
        {
            error = "У сохранения отсутствует имя слота";
            return false;
        }

        save.SavedAt = save.SavedAt ?? string.Empty;
        save.SceneName = save.SceneName != null ? save.SceneName.Trim() : string.Empty;
        save.Player ??= new PlayerSaveData();
        save.Recorder ??= new CloneRecorderSaveData();
        save.Carryables ??= new List<CarryableSaveData>();
        save.Components ??= new List<ComponentSaveData>();

        save.Player.Position = Finite(save.Player.Position, Vector3.zero);
        save.Player.Rotation = Rotation(save.Player.Rotation);
        save.Player.CameraPitch = Mathf.Clamp(Finite(save.Player.CameraPitch, 0f), -89f, 89f);
        save.Player.HeldItemPath ??= string.Empty;

        save.Recorder.RecordingTime = Mathf.Max(0f, Finite(save.Recorder.RecordingTime, 0f));
        save.Recorder.SavedRecordings ??= new List<RecordingSaveData>();
        save.Recorder.SavedRecordings.RemoveAll(item => item == null);

        save.Carryables.RemoveAll(item => item == null);
        foreach (CarryableSaveData item in save.Carryables)
        {
            item.Path ??= string.Empty;
            item.Position = Finite(item.Position, Vector3.zero);
            item.Rotation = Rotation(item.Rotation);
        }

        save.Components.RemoveAll(item => item == null);
        foreach (ComponentSaveData component in save.Components)
        {
            component.Path ??= string.Empty;
            component.ComponentType ??= string.Empty;
            component.ComponentIndex = Mathf.Max(0, component.ComponentIndex);
            component.Json = string.IsNullOrWhiteSpace(component.Json) ? "{}" : component.Json;

            if (!TryValidateJsonObject(component.Json, component.ComponentType))
            {
                error = $"Повреждено состояние компонента '{component.ComponentType}'";
                return false;
            }
        }

        return true;
    }

    public static float Finite(float value, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    }

    public static Vector3 Finite(Vector3 value, Vector3 fallback)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z)
            ? value
            : fallback;
    }

    public static Quaternion Rotation(Quaternion value)
    {
        if (!IsFinite(value.x) || !IsFinite(value.y) ||
            !IsFinite(value.z) || !IsFinite(value.w) ||
            value.x * value.x + value.y * value.y +
            value.z * value.z + value.w * value.w < 0.000001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.Normalize(value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool TryValidateJsonObject(string json, string componentType)
    {
        string trimmed = json.Trim();

        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
            return false;

        try
        {
            if (componentType == typeof(ObjectVisibilityAction).FullName)
                return JsonUtility.FromJson<ObjectVisibilityStateProbe>(trimmed) != null;
            if (componentType == typeof(FloorCubeLiftAction).FullName)
                return JsonUtility.FromJson<FloorLiftStateProbe>(trimmed) != null;
            if (componentType == typeof(HoldDoorAction).FullName)
                return JsonUtility.FromJson<DoorStateProbe>(trimmed) != null;

            return JsonUtility.FromJson<JsonObjectProbe>(trimmed) != null;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    [Serializable]
    private class JsonObjectProbe
    {
    }

    [Serializable]
    private class ObjectVisibilityStateProbe
    {
        public int ActiveRequests = 0;
        public bool[] InitialStates = Array.Empty<bool>();
        public bool[] CurrentStates = Array.Empty<bool>();
    }

    [Serializable]
    private class FloorLiftStateProbe
    {
        public int ActiveRequests = 0;
        public float CurrentHeight = 0f;
    }

    [Serializable]
    private class DoorStateProbe
    {
        public int ActiveSignals = 0;
        public float Progress = 0f;
    }
}
