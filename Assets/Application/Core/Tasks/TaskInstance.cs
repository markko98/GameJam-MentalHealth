using System;

[Serializable]
public class TaskInstance
{
    public string DefinitionId;
    public int CurrentProgress;
    public int TargetValue;
    public TaskCompletionState CompletionState = TaskCompletionState.NotCompleted;
}
