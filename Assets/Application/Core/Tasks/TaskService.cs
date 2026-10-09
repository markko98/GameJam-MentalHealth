using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

// Generic daily/weekly task system. Core has no idea what "kill enemies" or
// "collect gold" means — a game defines its own TaskDefinitions in a
// TaskCatalog asset and reports progress by calling ReportProgress(key) from
// its own gameplay code whenever something relevant happens. Rewards work the
// same way: RewardId/RewardAmount are opaque here; the game listens to
// OnRewardCollected and grants whatever that id means.
public static class TaskService
{
    public static Action<TaskInstance> OnTaskCompleted;
    public static Action<TaskInstance, TaskDefinition> OnRewardCollected;
    public static Action<TaskCategory> OnTasksRefreshed;

    private static TaskCatalog catalog;

    private static readonly Dictionary<TaskCategory, int> tasksPerCategory = new Dictionary<TaskCategory, int>
    {
        { TaskCategory.Daily, 3 },
        { TaskCategory.Weekly, 3 }
    };

    [Serializable]
    private class CategoryState
    {
        public string PeriodKey;
        public bool HasRerolled;
        public List<TaskInstance> Tasks = new List<TaskInstance>();
    }

    private static readonly Dictionary<TaskCategory, CategoryState> state = new Dictionary<TaskCategory, CategoryState>();

    public static void Initialize(TaskCatalog taskCatalog)
    {
        catalog = taskCatalog;

        foreach (TaskCategory category in Enum.GetValues(typeof(TaskCategory)))
        {
            EnsureFreshState(category);
        }
    }

    // Call before Initialize if a game wants a different pool size than the
    // default of 3 per category.
    public static void SetTaskCount(TaskCategory category, int count) => tasksPerCategory[category] = count;

    public static List<TaskInstance> GetTasks(TaskCategory category)
    {
        EnsureFreshState(category);
        return state[category].Tasks;
    }

    public static TaskDefinition GetDefinition(string definitionId) =>
        catalog.Definitions.FirstOrDefault(d => d.Id == definitionId);

    public static string GetDescription(TaskInstance task)
    {
        var definition = GetDefinition(task.DefinitionId);
        return definition == null ? string.Empty : string.Format(definition.DescriptionFormat, task.TargetValue);
    }

    public static bool AreAllTasksCompleted(TaskCategory category) =>
        GetTasks(category).All(t => t.CompletionState != TaskCompletionState.NotCompleted);

    // Shorthand for a TaskEvent with no parameters — e.g. from an enemy-death
    // handler: TaskService.ReportProgress("enemies_killed"). Only tasks whose
    // Conditions require a parameter would ever tell the difference from Report().
    public static void ReportProgress(string progressKey, int amount = 1) =>
        Report(TaskEvent.Create(progressKey, amount));

    // The general entry point gameplay code reports progress through. A task
    // only advances if its ProgressKey matches AND every one of its Conditions
    // (if any) is satisfied by the event's parameters.
    public static void Report(TaskEvent taskEvent)
    {
        foreach (TaskCategory category in Enum.GetValues(typeof(TaskCategory)))
        {
            var changed = false;

            foreach (var task in GetTasks(category))
            {
                if (task.CompletionState != TaskCompletionState.NotCompleted) continue;

                var definition = GetDefinition(task.DefinitionId);
                if (definition == null || definition.ProgressKey != taskEvent.ProgressKey) continue;
                if (!MatchesConditions(definition, taskEvent)) continue;

                task.CurrentProgress = Mathf.Min(task.CurrentProgress + taskEvent.Amount, task.TargetValue);
                changed = true;

                if (task.CurrentProgress >= task.TargetValue)
                {
                    task.CompletionState = TaskCompletionState.Completed;

                    OnTaskCompleted?.Invoke(task);
                }
            }

            if (changed) Save(category);
        }
    }

    private static bool MatchesConditions(TaskDefinition definition, TaskEvent taskEvent)
    {
        foreach (var condition in definition.Conditions)
        {
            if (string.IsNullOrEmpty(condition.ParamKey)) continue; // an unset condition row gates nothing

            taskEvent.TryGetValue(condition.ParamKey, out var actual);
            var isEqual = actual == condition.Value;

            var satisfied = condition.Operator == TaskConditionOperator.Equals ? isEqual : !isEqual;
            if (!satisfied) return false;
        }

        return true;
    }

    public static void CollectReward(TaskCategory category, TaskInstance task)
    {
        if (task.CompletionState != TaskCompletionState.Completed) return;

        task.CompletionState = TaskCompletionState.RewardCollected;
        Save(category);

        var definition = GetDefinition(task.DefinitionId);
        if (definition == null) return;

        OnRewardCollected?.Invoke(task, definition);
    }

    public static bool CanReroll(TaskCategory category)
    {
        EnsureFreshState(category);
        return !state[category].HasRerolled;
    }

    // Swaps one task for a different random pick from the catalog, once per period.
    public static void Reroll(TaskCategory category, TaskInstance taskToReplace)
    {
        if (!CanReroll(category)) return;

        var categoryState = state[category];
        var usedIds = categoryState.Tasks.Select(t => t.DefinitionId).ToList();
        var candidates = catalog.Definitions.Where(d => d.Category == category && !usedIds.Contains(d.Id)).ToList();
        if (candidates.Count == 0) return;

        var index = categoryState.Tasks.IndexOf(taskToReplace);
        if (index < 0) return;

        categoryState.Tasks[index] = CreateInstance(candidates.GetRandomElement());
        categoryState.HasRerolled = true;
        Save(category);
    }

    private static void EnsureFreshState(TaskCategory category)
    {
        if (!state.TryGetValue(category, out var categoryState))
        {
            categoryState = PersistenceService.Store.Load(StateKey(category), new CategoryState());
            state[category] = categoryState;
        }

        var currentPeriod = CurrentPeriodKey(category);
        if (categoryState.PeriodKey == currentPeriod && categoryState.Tasks.Count > 0) return;

        categoryState.PeriodKey = currentPeriod;
        categoryState.HasRerolled = false;
        categoryState.Tasks = GenerateTasks(category);
        Save(category);

        OnTasksRefreshed?.Invoke(category);
    }

    private static List<TaskInstance> GenerateTasks(TaskCategory category)
    {
        var pool = catalog.Definitions.Where(d => d.Category == category).ToList();
        var count = tasksPerCategory.TryGetValue(category, out var c) ? c : 3;
        var picks = new List<TaskInstance>();

        while (picks.Count < count && pool.Count > 0)
        {
            var pick = pool.GetRandomElement();
            pool.Remove(pick);
            picks.Add(CreateInstance(pick));
        }

        return picks;
    }

    private static TaskInstance CreateInstance(TaskDefinition definition) => new TaskInstance
    {
        DefinitionId = definition.Id,
        TargetValue = definition.TargetValue,
        CurrentProgress = 0,
        CompletionState = TaskCompletionState.NotCompleted
    };

    private static void Save(TaskCategory category) =>
        PersistenceService.Store.Save(StateKey(category), state[category]);

    private static string StateKey(TaskCategory category) => $"tasks_{category}".ToLowerInvariant();

    private static string CurrentPeriodKey(TaskCategory category)
    {
        var now = DateTime.UtcNow;

        return category == TaskCategory.Daily
            ? now.ToString("yyyy-MM-dd")
            : $"{now.Year}-W{CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
    }
}
