using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AppDelegate : MonoBehaviour
{
    public static bool appStarted;
    public static event Action OnApplicationBecomeActive;
    public static event Action OnApplicationWillBecomeInactive;
    public static event Action OnApplicationWillQuit;

    private void Start()
    {
        ApplicationStarted();
        appStarted = true;
        DontDestroyOnLoad(gameObject);
        
        AppDelegate.OnApplicationBecomeActive?.Invoke();
    }

    #if UNITY_IOS
    private void OnApplicationFocus(bool focus)
    {
        if(!appStarted) return;
        if (focus)
        {
            ApplicationIsRestoredFromBackground();
            AppDelegate.OnApplicationBecomeActive?.Invoke();
        }
        else
        {
            ApplicationWillGoToBackground();
            AppDelegate.OnApplicationWillBecomeInactive?.Invoke();
        }
    }
    #endif

    #if UNITY_ANDROID
    private void OnApplicationPause(bool pause)
    {
        if (!appStarted) return;
        if (pause)
        {
            ApplicationWillGoToBackground();
            AppDelegate.OnApplicationWillBecomeInactive?.Invoke();
            
        }
        else
        {
            ApplicationIsRestoredFromBackground();
            AppDelegate.OnApplicationBecomeActive?.Invoke();
        }
    }
    #endif

    private void OnApplicationQuit()
    {
        AppDelegate.OnApplicationWillQuit?.Invoke();
        ApplicationWillQuit();
    }

    public virtual void ApplicationWillQuit() { }
    public virtual void ApplicationWillGoToBackground() { }
    public virtual void ApplicationIsRestoredFromBackground() { }
    public virtual void ApplicationStarted() { }
}
