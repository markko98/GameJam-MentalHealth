#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Shows MonologueId as a dropdown of the catalogue's ids.
[CustomPropertyDrawer(typeof(MonologueId))]
public class MonologueIdDrawer : PropertyDrawer
{
    private const string NoneLabel = "<None>";

    private static MonologueCatalogue catalogue;
    private static readonly List<string> ids = new List<string>();
    private static readonly List<GUIContent> options = new List<GUIContent>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var valueProperty = property.FindPropertyRelative("value");

        if (catalogue == null) catalogue = MonologueIdsCodeGenerator.FindCatalogue();
        if (catalogue == null)
        {
            EditorGUI.PropertyField(position, valueProperty, label);
            return;
        }

        var current = valueProperty.stringValue ?? string.Empty;
        BuildOptions(current);

        var index = string.IsNullOrEmpty(current)
            ? 0
            : ids.FindIndex(id => string.Equals(id, current, StringComparison.OrdinalIgnoreCase));

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

        foreach (var monologue in catalogue.monologues)
        {
            if (monologue == null || ids.Contains(monologue.Id)) continue;
            ids.Add(monologue.Id);
            options.Add(new GUIContent(monologue.Id));
        }

        if (!string.IsNullOrEmpty(current) && !ids.Exists(id => string.Equals(id, current, StringComparison.OrdinalIgnoreCase)))
        {
            ids.Add(current);
            options.Add(new GUIContent($"{current} (missing)"));
        }
    }
}
#endif
