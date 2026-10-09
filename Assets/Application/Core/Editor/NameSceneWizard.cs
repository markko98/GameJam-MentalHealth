using UnityEditor;
using UnityEngine;

public class CreateSceneWizard : EditorWindow
{
    private string baseName = "NewScene";

    public static void Open()
    {
        var window = CreateInstance<CreateSceneWizard>();
        window.titleContent = new GUIContent("Create Scene Setup");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        GUILayout.Label("Base Name", EditorStyles.boldLabel);
        baseName = EditorGUILayout.TextField(baseName);

        GUILayout.Space(10);

        if (GUILayout.Button("Create"))
        {
            CreateSceneController.CreateSceneWithName(baseName);
            Close();
        }
    }
}