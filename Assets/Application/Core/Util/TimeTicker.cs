using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTicker : MonoBehaviour
{
    List<Action<float>> updateActions = new List<Action<float>>();
    List<Action<float>> fixedUpdateActions = new List<Action<float>>();

    private void Update()
    {
        for (int i = 0; i < updateActions.Count; i++)
        {
            updateActions[i](Time.deltaTime);
        }
    }
    private void FixedUpdate()
    {
        for (int i = 0; i < fixedUpdateActions.Count; i++)
        {
            fixedUpdateActions[i](Time.fixedDeltaTime);
        }
    }

    public void RegisterUpdate(Action<float> action)
    {
        updateActions.Add(action);
    }
    public void RegisterFixedUpdate(Action<float> action)
    {
        fixedUpdateActions.Add(action);
    }
}