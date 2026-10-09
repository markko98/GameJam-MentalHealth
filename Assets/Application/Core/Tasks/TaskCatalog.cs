using System.Collections.Generic;
using UnityEngine;

// Empty by default — each game fills this in via the Inspector with its own
// task types (and the ProgressKey strings its gameplay code reports against),
// so TaskService itself never changes between games.
[CreateAssetMenu(fileName = "TaskCatalog", menuName = "Tools/Task Catalog")]
public class TaskCatalog : ScriptableObject
{
    public List<TaskDefinition> Definitions = new List<TaskDefinition>();
}
