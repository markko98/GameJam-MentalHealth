using System;

// One filter on a TaskDefinition's progress reports, e.g. ParamKey "weapon"
// Equals "shotgun". A task with conditions only counts a matching-ProgressKey
// report if every one of its conditions also matches the reported TaskEvent —
// conditions on one task are always AND'd together.
[Serializable]
public class TaskCondition
{
    public string ParamKey;
    public TaskConditionOperator Operator = TaskConditionOperator.Equals;
    public string Value;
}
