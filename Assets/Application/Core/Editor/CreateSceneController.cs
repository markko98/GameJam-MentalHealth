using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

public static class CreateSceneController
{
    
    [MenuItem("Assets/Create/Tools/Scene Controller")]
    public static void OpenWizard()
    {
        CreateSceneWizard.Open();
    }
    
    // [MenuItem("Assets/Create/My Feature/Scene + Scripts", priority = 10)]
    public static void CreateSceneWithScripts()
    {
        string folderPath = GetSelectedFolderPath();

        // 1. Create Scene
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects,
            NewSceneMode.Single);

        string scenePath = Path.Combine(folderPath, "MyScene.unity");
        EditorSceneManager.SaveScene(scene, scenePath);

        // 2. Create Scripts
        CreateScriptFromTemplate(
            "Assets/ScriptTemplates/SceneController.txt",
            folderPath,
            "test",
            "USceneController.cs");

        CreateScriptFromTemplate(
            "Assets/ScriptTemplates/SceneOutlet.txt",
            folderPath,
            "test",
            "Outlet.cs");

        AssetDatabase.Refresh();
    }

    private static void CreateScriptFromTemplate(
        string templateAssetPath,
        string targetFolder,
        string baseName,
        string fileName)
    {
        var template = AssetDatabase.LoadAssetAtPath<TextAsset>(templateAssetPath);

        if (template == null)
        {
            Debug.LogError($"Template not found at path: {templateAssetPath}");
            return;
        }

        string scriptName = Path.GetFileNameWithoutExtension(fileName);
        string content = template.text
            .Replace("#SCRIPTNAME#", scriptName)
            .Replace("#BASENAME#", baseName);

        string fullPath = Path.Combine(targetFolder, fileName);
        File.WriteAllText(fullPath, content);
    }

    private static string GetSelectedFolderPath()
    {
        string path = "Assets";

        if (Selection.activeObject != null)
        {
            path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (Path.HasExtension(path))
                path = Path.GetDirectoryName(path);
        }

        return path?.Replace("\\", "/");
    }
    
    public static void CreateSceneWithName(string baseName)
    {
        string folderPath = GetSelectedFolderPath() + "/" + baseName;
        Directory.CreateDirectory(folderPath);
        
        // Scene
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects,
            NewSceneMode.Single);

        var scenePath = $"{folderPath}/{baseName}.unity";

        EditorSceneManager.SaveScene(
            scene,
            scenePath);
        
        AddSceneToBuildSettings(scenePath);
        
        UpdateSceneAndOutletNames(baseName);

        // Scripts
        CreateScriptFromTemplate(
            "Assets/ScriptTemplates/SceneController.txt",
            folderPath,
            baseName,
            $"{baseName}SceneController.cs");

        CreateScriptFromTemplate(
            "Assets/ScriptTemplates/SceneOutlet.txt",
            folderPath,
            baseName,
            $"{baseName}Outlet.cs");
        
        AssetDatabase.Refresh();
    }
    
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void CreateAssetWhenReady()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += CreateAssetWhenReady;
            return;
        }

        EditorApplication.delayCall += AddOutletToScene;
    }

    private static void AddOutletToScene()
    {
        var scene = SceneManager.GetActiveScene();
        var outletName = $"{scene.name}Outlet";

        if (scene.GetRootGameObjects().Any(go => go.name == outletName)) return;
        
        var outlet = new GameObject(outletName);
        var type = Type.GetType(outletName) ?? Type.GetType(outletName + ",Assembly-CSharp");
        outlet.AddComponent(type);
        
        SceneManager.MoveGameObjectToScene(outlet, scene);
        EditorSceneManager.SaveScene(scene, scene.path);
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var currentScenes = EditorBuildSettings.scenes;

        if (currentScenes.Any(s => s.path == scenePath))
        {
            Debug.Log("Scene already exists in Build Settings.");
            return;
        }

        var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
        currentScenes.CopyTo(newScenes, 0);

        newScenes[^1] = new EditorBuildSettingsScene(scenePath, true);

        EditorBuildSettings.scenes = newScenes;
    }
    
    private static void UpdateSceneAndOutletNames(string field)
    {
        var classPath = "Assets/Application/Core/Scene Framework/SceneControllers/Scripts/";


        var sceneNamesPath = classPath + "SceneNames.cs";
        UpdateClass(sceneNamesPath, field, field);
        
        var outletNamesPath = classPath + "OutletNames.cs";
        UpdateClass(outletNamesPath, field, field + "Outlet");
    }

    private static void UpdateClass(string classPath, string fieldName, string fieldValue)
    {
        var lines = File.ReadAllLines(classPath);
        var insertIndex = lines.Length - 1;
        
        var newLine = $"    public static string {fieldName} = \"{fieldValue}\";";
        var updatedLines = new List<string>(lines);
        updatedLines.Insert(insertIndex, newLine);

        File.WriteAllLines(classPath, updatedLines);

        AssetDatabase.Refresh();
    }

}
