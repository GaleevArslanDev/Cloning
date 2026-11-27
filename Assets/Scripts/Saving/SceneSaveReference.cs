using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneSaveReference
{
    public static string GetPath(Component component)
    {
        return component != null ? GetPath(component.transform) : string.Empty;
    }

    public static string GetPath(Transform target)
    {
        if (target == null || !target.gameObject.scene.IsValid())
            return string.Empty;

        string path = target.GetSiblingIndex().ToString();
        Transform parent = target.parent;

        while (parent != null)
        {
            path = parent.GetSiblingIndex() + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    public static Transform FindTransform(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string[] parts = path.Split('/');

        if (!int.TryParse(parts[0], out int rootIndex))
            return null;

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        if (rootIndex < 0 || rootIndex >= roots.Length)
            return null;

        Transform current = roots[rootIndex].transform;

        for (int i = 1; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int childIndex) ||
                childIndex < 0 || childIndex >= current.childCount)
            {
                return null;
            }

            current = current.GetChild(childIndex);
        }

        return current;
    }

    public static T FindComponent<T>(string path) where T : Component
    {
        Transform target = FindTransform(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    public static MonoBehaviour FindBehaviour(
        string path,
        string componentType,
        int componentIndex
    )
    {
        Transform target = FindTransform(path);

        if (target == null ||
            string.IsNullOrWhiteSpace(componentType) ||
            componentIndex < 0)
            return null;

        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        int matchingIndex = 0;

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour.GetType().FullName != componentType)
                continue;

            if (matchingIndex == componentIndex)
                return behaviour;

            matchingIndex++;
        }

        return null;
    }

    public static int GetComponentIndex(MonoBehaviour target)
    {
        if (target == null)
            return 0;

        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();
        int index = 0;

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == target)
                return index;

            if (behaviour != null && behaviour.GetType() == target.GetType())
                index++;
        }

        return 0;
    }
}
