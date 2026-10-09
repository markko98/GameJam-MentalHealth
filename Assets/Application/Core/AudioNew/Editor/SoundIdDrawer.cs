#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Shows SoundId fields as a dropdown of the catalog's ids instead of a free text field.
[CustomPropertyDrawer(typeof(SoundId))]
public class SoundIdDrawer : PropertyDrawer
{
    private const string NoneLabel = "<None>";

    private static SoundCatalog catalog;
    private static readonly List<string> ids = new List<string>();
    private static readonly List<GUIContent> options = new List<GUIContent>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var valueProperty = property.FindPropertyRelative("value");

        if (catalog == null) catalog = SoundIdsCodeGenerator.FindCatalog();
        if (catalog == null)
        {
            EditorGUI.PropertyField(position, valueProperty, label);
            return;
        }

        var current = valueProperty.stringValue ?? string.Empty;
        BuildOptions(current);

        var index = string.IsNullOrEmpty(current) ? 0 : ids.FindIndex(id => string.Equals(id, current, System.StringComparison.OrdinalIgnoreCase));

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        var picked = EditorGUI.Popup(position, label, Mathf.Max(0, index), options.ToArray());
        if (EditorGUI.EndChangeCheck()) valueProperty.stringValue = ids[picked];
        EditorGUI.EndProperty();
    }

    private static void BuildOptions(string current)
    {
        ids.Clear();
        options.Clear();

        ids.Add(string.Empty);
        options.Add(new GUIContent(NoneLabel));

        foreach (var sound in catalog.sounds)
        {
            if (sound == null || ids.Contains(sound.Id)) continue;
            ids.Add(sound.Id);
            options.Add(new GUIContent(sound.Id));
        }

        // Keep an id that was removed from the catalog visible instead of silently resetting it.
        if (!string.IsNullOrEmpty(current) && !ids.Exists(id => string.Equals(id, current, System.StringComparison.OrdinalIgnoreCase)))
        {
            ids.Add(current);
            options.Add(new GUIContent($"{current} (missing)"));
        }
    }
}
#endif
