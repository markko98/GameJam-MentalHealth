using System.Collections;
using UnityEngine;
using System;

public class GameTicker
{
    public static float TimeScale = 1f;
    public static float DeltaTime;
    public static float FixedDeltaTime;
    public TimeTicker Ticker;
    public event Action Update;
    public event Action FixedUpdate;

    public static GameTicker SharedInstance
    {
        get
        {
            if (_sharedInstance == null)
            {
                Initialize();
            }

            return _sharedInstance;
        }
    }

    private static GameTicker _sharedInstance;

    public static void Initialize()
    {
        if (_sharedInstance != null) return;
        
        var tickerObject = new GameObject();
        tickerObject.name = "TimeTicker";
        GameObject.DontDestroyOnLoad(tickerObject);

        _sharedInstance = new GameTicker();
        _sharedInstance.Ticker = tickerObject.AddComponent<TimeTicker>();
        _sharedInstance.Ticker.RegisterUpdate(_sharedInstance.update);
        _sharedInstance.Ticker.RegisterFixedUpdate(_sharedInstance.fixedUpdate);
        
    }
    private void update(float deltaTime)
    {
        DeltaTime = deltaTime * TimeScale;

        Update?.Invoke();
    }
    private void fixedUpdate(float fixedDeltaTime)
    {
        FixedDeltaTime = fixedDeltaTime * TimeScale;

        FixedUpdate?.Invoke();
    }
}