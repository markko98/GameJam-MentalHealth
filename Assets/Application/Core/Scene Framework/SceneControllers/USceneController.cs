using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public interface ISceneController
{
    string SceneName { get; set; }
    bool IsChild { get; set; }
    bool IsLoaded { get;  }
    bool IsRegisteredLoad { get; }

     USceneController ParentSceneController { get; }
}

public class USceneController : ISceneController
{
    public string SceneName { get; set; }
    public bool IsChild { get; set; }
    public bool IsLoaded { get; private set; }
    public bool IsRegisteredLoad { get; private set; }
    public USceneController ParentSceneController { get; set; }

    private List<USceneController> ChildControllers = new List<USceneController>();

    public USceneController(string sceneName)
    {
        SceneName = sceneName; 
    }

    private void RegisterLoadCompleted(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneName)
        {
            IsLoaded = true;
            SceneDidLoad();
            UnregisterLoad();
        }
    }

    //scene stack management
    /// <summary>
    /// Load given scene
    /// </summary>
    /// <param name="controller"></param>
    public void PushSceneController(USceneController controller)
    {
        if (controller == null) return;
        if (IsChild)
        {
            ParentSceneController.PushSceneController(controller);
            return;
        }
        
        controller.RegisterLoad();
        controller.SceneWillAppear();
        UnregisterLoad();
        var childControllers = GetChildScenes(this);
        
        foreach (var sceneController in childControllers)
        {
            RemoveChildSceneController(sceneController);
        }
        
        SceneWillDisappear();
        
        controller.ParentSceneController = this;

        UNavigationController.PresentViewController(controller);
    }

    private List<USceneController> GetChildScenes(USceneController controller, List<USceneController> tempControllers = null)
    {
        if (tempControllers == null)
        {
            tempControllers = new List<USceneController>();
        }
        foreach (var childController in controller.ChildControllers)
        {
            tempControllers.Add(childController);
            GetChildScenes(childController, tempControllers);
        }

        return tempControllers;
    }

    /// <summary>
    /// Load scene additive
    /// </summary>
    /// <param name="controller"></param>
    public void AddChildSceneController(USceneController controller)
    {
        controller.IsChild = true;
        controller.RegisterLoad();
        controller.SceneWillAppear();
        controller.ParentSceneController = this;
        ChildControllers.Add(controller);
        SceneManager.LoadSceneAsync(controller.SceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// Remove scene that is loaded additively
    /// </summary>
    public void RemoveFromParentSceneController()
    {
        ParentSceneController.RemoveChildSceneController(this);
    }

    public void RemoveChildSceneController(USceneController controller)
    {
        controller.ParentSceneController.ChildControllers.Remove(controller);
        controller.ParentSceneController = null;
        controller.SceneWillDisappear();
        SceneManager.UnloadSceneAsync(controller.SceneName);
    }

    /// <summary>
    /// Close current scene and remove it from stack
    /// </summary>
    public void PopToParentSceneController()
    {
        if (UNavigationController.ActiveController == this)
        {
            UNavigationController.RemoveViewController();
        }
    }

    //Life cycle
    public virtual void SceneDidLoad()
    {
        // Debug.Log("ViewDidLoad: " + SceneName);
    }

    public virtual void SceneWillDisappear()
    {
        
        // Debug.Log("ViewWillDisappear:" + SceneName);
    }

    public virtual void SceneWillAppear()
    {
        // Debug.Log("ViewWillAppear:" + SceneName);
    }

    public void RegisterLoad()
    {
        if (IsRegisteredLoad) return;

        IsRegisteredLoad = true;
        SceneManager.sceneLoaded += RegisterLoadCompleted;
    }

    public void UnregisterLoad()
    {
        if (!IsRegisteredLoad) return;

        IsRegisteredLoad = false;
        SceneManager.sceneLoaded -= RegisterLoadCompleted;
    }

    //unregister
    ~USceneController()
    {
        UnregisterLoad();
    }
}

