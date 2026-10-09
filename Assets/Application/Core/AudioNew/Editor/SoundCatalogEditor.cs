#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundCatalog))]
public class SoundCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();

        if (GUILayout.Button("Add All Sound Definitions In Project"))
            AddAllDefinitions((SoundCatalog)target);

        if (GUILayout.Button("Regenerate SoundIds"))
            SoundIdsCodeGenerator.Generate((SoundCatalog)target);
    }

    // Keeps SoundIds in sync without a manual click - fires once editing pauses (selection
    // change or Inspector closed) instead of regenerating and recompiling on every keystroke.
    private void OnDisable() => SoundIdsCodeGenerator.Generate(target as SoundCatalog);

    private static void AddAllDefinitions(SoundCatalog catalog)
    {
        var missing = AssetDatabase.FindAssets("t:SoundDefinition")
            .Select(guid => AssetDatabase.LoadAssetAtPath<SoundDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(definition => definition != null && !catalog.sounds.Contains(definition))
            .ToList();

        if (missing.Count == 0) return;

        Undo.RecordObject(catalog, "Add Sound Definitions");
        catalog.sounds.RemoveAll(definition => definition == null);
        catalog.sounds.AddRange(missing);
        EditorUtility.SetDirty(catalog);
    }
}
#endif
