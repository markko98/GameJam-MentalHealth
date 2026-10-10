#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonologueCatalogue))]
public class MonologueCatalogueEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();

        var catalogue = (MonologueCatalogue)target;

        if (catalogue.viewPrefab == null)
            EditorGUILayout.HelpBox("No view prefab: run Tools > Create > Monologue System to build one.", MessageType.Warning);

        if (GUILayout.Button("Add All Monologues In Project"))
            AddAllDefinitions(catalogue);

        if (GUILayout.Button("Regenerate MonologueIds"))
            MonologueIdsCodeGenerator.Generate(catalogue);
    }

    private void OnDisable() => MonologueIdsCodeGenerator.Generate(target as MonologueCatalogue);

    private static void AddAllDefinitions(MonologueCatalogue catalogue)
    {
        var missing = AssetDatabase.FindAssets("t:MonologueDefinition")
            .Select(guid => AssetDatabase.LoadAssetAtPath<MonologueDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(definition => definition != null && !catalogue.monologues.Contains(definition))
            .ToList();

        if (missing.Count == 0) return;

        Undo.RecordObject(catalogue, "Add Monologues");
        catalogue.monologues.RemoveAll(definition => definition == null);
        catalogue.monologues.AddRange(missing);
        EditorUtility.SetDirty(catalogue);
    }
}
#endif
