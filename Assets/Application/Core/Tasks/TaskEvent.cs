using System.Collections.Generic;
using System.Globalization;

// Fluent builder for a progress report, so gameplay code can attach context
// (weapon, map, ...) for a TaskDefinition's Conditions to filter on:
//
//   TaskService.Report(TaskEvent.Create(TaskProgressKeys.Kill)
//       .With(TaskParamKeys.Weapon, "shotgun")
//       .With(TaskParamKeys.Map, "forest"));
//
// TaskService.ReportProgress(key, amount) still exists as a shorthand for a
// TaskEvent with no parameters — only tasks with matching Conditions (if any)
// care about the difference.
public class TaskEvent
{
    public string ProgressKey { get; }
    public int Amount { get; }

    private readonly List<(string Key, string Value)> parameters = new List<(string, string)>();

    private TaskEvent(string progressKey, int amount)
    {
        ProgressKey = progressKey;
        Amount = amount;
    }

    public static TaskEvent Create(string progressKey, int amount = 1) => new TaskEvent(progressKey, amount);

    public TaskEvent With(string paramKey, string value)
    {
        parameters.Add((paramKey, value));
        return this;
    }

    public TaskEvent With(string paramKey, int value) => With(paramKey, value.ToString(CultureInfo.InvariantCulture));

    public TaskEvent With(string paramKey, float value) => With(paramKey, value.ToString(CultureInfo.InvariantCulture));

    public bool TryGetValue(string paramKey, out string value)
    {
        foreach (var pair in parameters)
        {
            if (pair.Key != paramKey) continue;
            value = pair.Value;
            return true;
        }

        value = null;
        return false;
    }
}
