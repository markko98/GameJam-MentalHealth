#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(UIToggle), true)]
[CanEditMultipleObjects]
public class UIToggleEditor : ToggleEditor
{
    private static readonly string[] Fields = { "label", "clickSound", "disabledLabelAlpha" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        foreach (var field in Fields) EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }
}
#endif
