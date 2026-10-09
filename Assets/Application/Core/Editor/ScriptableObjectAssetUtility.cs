using System.IO;
using UnityEditor;
using UnityEngine;

// Shared by editor tools that create/locate config ScriptableObjects
// (ConfigureWindow, TaskEditorWindow, ...).
public static class ScriptableObjectAssetUtility
{
    public static T CreateAssetIfMissing<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        EnsureFolder(Path.GetDirectoryName(path).Replace("\\", "/"));

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        var current = parts[0];

        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
