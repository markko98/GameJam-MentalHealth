using System;
using System.Collections.Generic;
using UnityEngine;

// A game defines its own pool of these in a TaskCatalog. ProgressKey is an
// arbitrary string the game invents and reports against from its own gameplay
// code (e.g. "enemies_killed", "coins_collected") — Core never interprets it.
// RewardId/RewardAmount are equally opaque; the game grants whatever they mean.
//
// Conditions narrow which TaskEvent reports count: with none, any report for
// ProgressKey counts (the original behavior). With some, a report only counts
// if it also carries matching values for every condition's ParamKey — e.g.
// ProgressKey "kill" + conditions [weapon Equals "shotgun", map Equals "forest"]
// only progresses on a kill reported with those exact parameters.
[Serializable]
public class TaskDefinition
{
    public string Id;
    public string ProgressKey;
    public int TargetValue = 1;
    public TaskCategory Category;

    public List<TaskCondition> Conditions = new List<TaskCondition>();

    public string RewardId;
    public int RewardAmount;

    [TextArea]
    [Tooltip("Use {0} for the target value, e.g. \"Kill {0} enemies\"")]
    public string DescriptionFormat;
}
