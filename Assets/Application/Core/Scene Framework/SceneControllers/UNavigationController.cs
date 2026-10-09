using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UNavigationController
{
    private static readonly Stack<USceneController> _controllersStack = new Stack<USceneController>();
    public static USceneController ActiveController => _controllersStack.Count > 0 ? _controllersStack.Peek() : null;
    public static USceneController RootController { get; private set; }

    public static FadeConfig fadeConfigDefault = new FadeConfig() { FadeDuration = 0.25f, FadeColor = Color.black };
    public static bool UseFadeByDefault = true;
    private static string FadeCanvasResourcesPath = "SceneFadeTransitions";
    private static SceneFadeTransition currentFade;

    /// <summary>
    /// Open scene and add it to stack
    /// </summary>
    /// <param name="controller"></param>
    public static void PresentViewController(USceneController controller, bool? withFade = null,
        FadeConfig fadeConfig = null)
    {
        bool fade = withFade ?? UseFadeByDefault;

        if (_controllersStack.Count == 0)
        {
            RootController = controller;
        }

        _controllersStack.Push(controller);

        controller.RegisterLoad();
        controller.SceneWillAppear();

        if (fade)
        {
            var config = fadeConfig ?? fadeConfigDefault;
            var fadeComp = EnsureFadeInstance();
            if (fadeComp != null)
            {
                fadeComp.RunFadeTransition(
                    controller.SceneName,
                    LoadSceneMode.Single,
                    config.FadeDuration,
                    config.FadeColor
                );
            }
            else
            {
                SceneManager.LoadScene(controller.SceneName);
            }
        }
        else
        {
            SceneManager.LoadScene(controller.SceneName);
        }
    }

    /// <summary>
    /// Close current scene and remove it from stack
    /// </summary>
    public static void RemoveViewController(bool? withFade = null, FadeConfig fadeConfig = null)
    {
        var fade = withFade ?? UseFadeByDefault;

        var current = ActiveController;
        if (current == null) return;

        current.SceneWillDisappear();

        if (_controllersStack.Count > 0) _controllersStack.Pop();

        var target = ActiveController ?? RootController;
        if (target == null)
        {
            Debug.LogWarning("No target controller to load.");
            return;
        }

        target.RegisterLoad();
        target.SceneWillAppear();

        if (fade)
        {
            var config = fadeConfig ?? fadeConfigDefault;
            var fadeComp = EnsureFadeInstance();
            if (fadeComp != null)
            {
                fadeComp.RunFadeTransition(
                    target.SceneName,
                    LoadSceneMode.Single,
                    config.FadeDuration,
                    config.FadeColor
                );
            }
            else
            {
                SceneManager.LoadScene(target.SceneName);
            }
        }
        else
        {
            SceneManager.LoadScene(target.SceneName);
        }
    }

    /// <summary>
    /// Set root scene
    /// </summary>
    /// <param name="controller"></param>
    public static void SetRootViewController(USceneController controller, bool? withFade = null, FadeConfig fadeConfig = null)
    {
        ActiveController?.SceneWillDisappear();
        _controllersStack.Clear();

        controller.RegisterLoad();
        controller.SceneWillAppear();
        PresentViewController(controller, withFade, fadeConfig);
    }

    /// <summary>
    /// Load root scene
    /// </summary>
    public static void PopToRootViewController(bool? withFade = null, FadeConfig fadeConfig = null)
    {
        ActiveController.SceneWillDisappear();
        _controllersStack.Clear();
        RootController.SceneWillAppear();
        RootController.RegisterLoad();
        PresentViewController(RootController, withFade, fadeConfig);
    }

    private static SceneFadeTransition EnsureFadeInstance()
    {
        if (currentFade != null) return currentFade;

        var prefab = Resources.Load<GameObject>(FadeCanvasResourcesPath);
        if (prefab == null)
        {
            Debug.LogError($"UNavigationController: Fade prefab not found at 'Resources/{FadeCanvasResourcesPath}'.");
            return null;
        }

        var go = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(go);

        currentFade = go.GetComponent<SceneFadeTransition>();
        if (currentFade == null)
        {
            Debug.LogError("UNavigationController: The fade prefab is missing a SceneFadeTransition component.");
            return null;
        }

        currentFade.Initialize(fadeConfigDefault, null);
        return currentFade;
    }
}
