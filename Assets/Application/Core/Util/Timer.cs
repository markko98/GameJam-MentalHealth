using System;
using UnityEngine;

[Serializable]
public class Timer
{
    private readonly float timerGoal;
    private readonly bool triggerOnce;
    
    private float timeLimit; // Total time in seconds
    private float timer;
    private bool canUpdate;
    private bool timerTriggered = false;
    private bool isPlainTimer;
    
    private Action onGoalReached;
    public Action<float> TimeChanged;

    public Timer()
    {
        isPlainTimer = true;
        this.timerGoal = 0;
        this.onGoalReached = null;
        this.triggerOnce = false;
        this.timer = 0;
        this.timeLimit = 0;
        canUpdate = true;
        GameTicker.SharedInstance.Update += Update;
    }
    public Timer(float timerGoal, Action onGoalReached, bool startOnCreation = false, bool triggerOnce = true, bool isPlainTimer = false)
    {
        this.timerGoal = timerGoal;
        this.isPlainTimer = isPlainTimer;
        this.onGoalReached = onGoalReached;
        this.triggerOnce = triggerOnce;
        this.timer = 0;
        this.timeLimit = timerGoal;
        canUpdate = startOnCreation;
        GameTicker.SharedInstance.Update += Update;
    }

    private void Update()
    {
        if(canUpdate == false) return;
        if(triggerOnce && timerTriggered) return;
        
        timer += GameTicker.DeltaTime;
        if (timer >= timeLimit && isPlainTimer == false)
        {
            timer -= timeLimit;
            timerTriggered = true;
            onGoalReached?.Invoke();
        }
        TimeChanged?.Invoke(timeLimit - timer);
    }
    public void Start()
    {
        canUpdate = true;
    }
    public void Stop()
    {
        canUpdate = false;
    }
    public float GetCurrentTime()
    {
        return timer;
    }
    public int GetLeftOverSeconds()
    {
        return Mathf.FloorToInt(timeLimit - timer);
    }
    public void DecreaseTime(int amount)
    {
        timer += amount;
        TimeChanged?.Invoke(timeLimit - timer);
    }   
    public void IncreaseTime(int amount)
    {
        timeLimit += amount;
        TimeChanged?.Invoke(timeLimit);
    }
    public void ResetTimer()
    {
        timeLimit = timerGoal;
        timer = 0;
    }
    public void Cleanup()
    {
        Stop();
        GameTicker.SharedInstance.Update -= Update;
        timerTriggered = false;
        canUpdate = false;
        timer = 0;
        onGoalReached = null;
    }
    
    ~Timer()
    {
        Cleanup();
    }
}