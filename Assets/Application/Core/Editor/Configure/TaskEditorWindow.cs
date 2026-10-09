using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Authors TaskDefinitions directly against a TaskCatalog's SerializedObject —
// every edit applies and saves to the asset immediately, so there's no
// separate "save the catalog" step to remember.
public class TaskEditorWindow : EditorWindow
{
    private const string DefaultCatalogPath = "Assets/Application/Modules/Config/TaskCatalog.asset";

    private TaskCatalog catalog;
    private SerializedObject serializedCatalog;
    private SerializedProperty definitionsProperty;

    private Vector2 scroll;
    private bool showDaily = true;
    private bool showWeekly = true;

    private int pendingRemoveIndex = -1;
    private int pendingDuplicateIndex = -1;
    private bool structuralChangePending;

    // Inline "type a new id" entry state for the id pickers (ProgressKey, RewardId,
    // and per-condition ParamKey/Value) — only one field across the whole window can
    // be in this mode at a time, identified by the unique key passed to DrawIdField.
    private string newIdEditingKey;
    private string newIdBuffer = "";

    [MenuItem("Tools/Task Editor")]
    public static void Open()
    {
        GetWindow<TaskEditorWindow>("Task Editor");
    }

    private void OnEnable()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TaskCatalog>(DefaultCatalogPath);
        if (existing != null) SetCatalog(existing);
    }

    // Regenerating on every keystroke would recompile mid-edit and kick focus out of
    // whatever field is being typed in, so the automatic refresh happens on natural
    // "done editing for now" checkpoints (losing focus, closing the window) instead —
    // Add/Duplicate/Remove call it directly since those are discrete, not continuous.
    private void OnLostFocus() => TaskIdsCodeGenerator.Generate(catalog);

    private void OnDisable() => TaskIdsCodeGenerator.Generate(catalog);

    private void SetCatalog(TaskCatalog newCatalog)
    {
        catalog = newCatalog;
        serializedCatalog = catalog != null ? new SerializedObject(catalog) : null;
        definitionsProperty = serializedCatalog?.FindProperty(nameof(TaskCatalog.Definitions));
    }

    private void OnGUI()
    {
        DrawCatalogSelector();

        if (catalog == null)
        {
            EditorGUILayout.HelpBox("Assign or create a TaskCatalog to edit.", MessageType.Info);
            return;
        }

        serializedCatalog.Update();

        DrawToolbar();
        DrawTaskList();

        if (serializedCatalog.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            // Remove/Duplicate only take effect once applied above, unlike Add (which
            // applies itself immediately) — regenerate now that catalog.Definitions
            // actually reflects the change. Continuous edits (typing in a field) apply
            // here too but don't set this flag, so they don't trigger a mid-edit recompile.
            if (structuralChangePending)
            {
                structuralChangePending = false;
                TaskIdsCodeGenerator.Generate(catalog);
            }
        }

        DrawValidation();
    }

    private void DrawCatalogSelector()
    {
        EditorGUI.BeginChangeCheck();
        var picked = (TaskCatalog)EditorGUILayout.ObjectField("Task Catalog", catalog, typeof(TaskCatalog), false);
        if (EditorGUI.EndChangeCheck())
        {
            SetCatalog(picked);
        }

        if (catalog == null && GUILayout.Button("Create Task Catalog"))
        {
            SetCatalog(ScriptableObjectAssetUtility.CreateAssetIfMissing<TaskCatalog>(DefaultCatalogPath));
        }

        EditorGUILayout.Space();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            showDaily = GUILayout.Toggle(showDaily, "Daily", EditorStyles.toolbarButton, GUILayout.Width(60));
            showWeekly = GUILayout.Toggle(showWeekly, "Weekly", EditorStyles.toolbarButton, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{definitionsProperty.arraySize} task(s)", EditorStyles.miniLabel);
            if (GUILayout.Button("Add Task", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                AddTask();
            }
        }
    }

    private void AddTask()
    {
        definitionsProperty.arraySize++;
        var element = definitionsProperty.GetArrayElementAtIndex(definitionsProperty.arraySize - 1);

        // Growing a SerializedProperty array duplicates the last element's
        // values into the new slot — reset them so it doesn't look copied.
        element.FindPropertyRelative(nameof(TaskDefinition.Id)).stringValue = $"task_{definitionsProperty.arraySize}";
        element.FindPropertyRelative(nameof(TaskDefinition.ProgressKey)).stringValue = "";
        element.FindPropertyRelative(nameof(TaskDefinition.TargetValue)).intValue = 1;
        element.FindPropertyRelative(nameof(TaskDefinition.Category)).enumValueIndex = (int)TaskCategory.Daily;
        element.FindPropertyRelative(nameof(TaskDefinition.RewardId)).stringValue = "";
        element.FindPropertyRelative(nameof(TaskDefinition.RewardAmount)).intValue = 0;
        element.FindPropertyRelative(nameof(TaskDefinition.DescriptionFormat)).stringValue = "";

        serializedCatalog.ApplyModifiedProperties();
        TaskIdsCodeGenerator.Generate(catalog);
    }

    private void DrawTaskList()
    {
        pendingRemoveIndex = -1;
        pendingDuplicateIndex = -1;

        var knownProgressKeys = KnownValues(d => d.ProgressKey);
        var knownRewardIds = KnownValues(d => d.RewardId);
        var knownParamKeys = catalog.Definitions
            .SelectMany(d => d.Conditions)
            .Select(c => c.ParamKey)
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        var knownConditionValuesByParamKey = catalog.Definitions
            .SelectMany(d => d.Conditions)
            .Where(c => !string.IsNullOrEmpty(c.ParamKey) && !string.IsNullOrEmpty(c.Value))
            .GroupBy(c => c.ParamKey)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Value).Distinct().OrderBy(v => v).ToList());

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (var i = 0; i < definitionsProperty.arraySize; i++)
        {
            var element = definitionsProperty.GetArrayElementAtIndex(i);
            var category = (TaskCategory)element.FindPropertyRelative(nameof(TaskDefinition.Category)).enumValueIndex;

            if (category == TaskCategory.Daily && !showDaily) continue;
            if (category == TaskCategory.Weekly && !showWeekly) continue;

            DrawTaskElement(element, i, knownProgressKeys, knownRewardIds, knownParamKeys, knownConditionValuesByParamKey);
        }

        EditorGUILayout.EndScrollView();

        if (pendingRemoveIndex >= 0)
        {
            definitionsProperty.DeleteArrayElementAtIndex(pendingRemoveIndex);
            structuralChangePending = true;
        }
        else if (pendingDuplicateIndex >= 0)
        {
            definitionsProperty.InsertArrayElementAtIndex(pendingDuplicateIndex);
            structuralChangePending = true;
        }
    }

    private void DrawTaskElement(SerializedProperty element, int index, List<string> knownProgressKeys, List<string> knownRewardIds,
        List<string> knownParamKeys, Dictionary<string, List<string>> knownConditionValuesByParamKey)
    {
        var idProp = element.FindPropertyRelative(nameof(TaskDefinition.Id));
        var progressKeyProp = element.FindPropertyRelative(nameof(TaskDefinition.ProgressKey));
        var targetValueProp = element.FindPropertyRelative(nameof(TaskDefinition.TargetValue));
        var categoryProp = element.FindPropertyRelative(nameof(TaskDefinition.Category));
        var rewardIdProp = element.FindPropertyRelative(nameof(TaskDefinition.RewardId));
        var rewardAmountProp = element.FindPropertyRelative(nameof(TaskDefinition.RewardAmount));
        var descriptionProp = element.FindPropertyRelative(nameof(TaskDefinition.DescriptionFormat));

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var title = string.IsNullOrEmpty(idProp.stringValue) ? $"Task {index}" : idProp.stringValue;
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Duplicate", GUILayout.Width(70)))
                {
                    pendingDuplicateIndex = index;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    pendingRemoveIndex = index;
                }
            }

            EditorGUILayout.PropertyField(idProp);
            DrawIdField("Progress Key", $"t{index}_progress", progressKeyProp, knownProgressKeys);
            EditorGUILayout.PropertyField(categoryProp);
            EditorGUILayout.PropertyField(targetValueProp);
            DrawIdField("Reward Id", $"t{index}_reward", rewardIdProp, knownRewardIds);
            EditorGUILayout.PropertyField(rewardAmountProp);
            EditorGUILayout.PropertyField(descriptionProp);

            if (!string.IsNullOrEmpty(descriptionProp.stringValue))
            {
                EditorGUILayout.LabelField("Preview", SafeFormat(descriptionProp.stringValue, targetValueProp.intValue));
            }

            EditorGUILayout.Space(2);
            DrawConditions(element, index, knownParamKeys, knownConditionValuesByParamKey);
        }

        EditorGUILayout.Space(4);
    }

    // Builder for TaskDefinition.Conditions — every condition here must match a
    // TaskEvent's parameters (AND) for a progress report to count toward this task.
    private void DrawConditions(SerializedProperty element, int taskIndex, List<string> knownParamKeys,
        Dictionary<string, List<string>> knownConditionValuesByParamKey)
    {
        var conditionsProp = element.FindPropertyRelative(nameof(TaskDefinition.Conditions));

        EditorGUILayout.LabelField("Conditions (all must match)", EditorStyles.miniBoldLabel);

        var removeIndex = -1;

        for (var c = 0; c < conditionsProp.arraySize; c++)
        {
            var conditionElement = conditionsProp.GetArrayElementAtIndex(c);
            var paramKeyProp = conditionElement.FindPropertyRelative(nameof(TaskCondition.ParamKey));
            var operatorProp = conditionElement.FindPropertyRelative(nameof(TaskCondition.Operator));
            var valueProp = conditionElement.FindPropertyRelative(nameof(TaskCondition.Value));

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Condition {c + 1}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        removeIndex = c;
                    }
                }

                DrawIdField("Param", $"t{taskIndex}_c{c}_key", paramKeyProp, knownParamKeys);
                EditorGUILayout.PropertyField(operatorProp, new GUIContent("Comparison"));

                var currentParamKey = paramKeyProp.stringValue;
                var knownValues = !string.IsNullOrEmpty(currentParamKey) &&
                    knownConditionValuesByParamKey.TryGetValue(currentParamKey, out var values)
                        ? values
                        : new List<string>();

                DrawIdField("Value", $"t{taskIndex}_c{c}_value", valueProp, knownValues);
            }
        }

        if (removeIndex >= 0)
        {
            conditionsProp.DeleteArrayElementAtIndex(removeIndex);
            structuralChangePending = true;
        }

        if (GUILayout.Button("+ Add Condition", GUILayout.Width(120)))
        {
            conditionsProp.arraySize++;
            var newElement = conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1);
            newElement.FindPropertyRelative(nameof(TaskCondition.ParamKey)).stringValue = "";
            newElement.FindPropertyRelative(nameof(TaskCondition.Operator)).enumValueIndex = (int)TaskConditionOperator.Equals;
            newElement.FindPropertyRelative(nameof(TaskCondition.Value)).stringValue = "";
        }
    }

    // Every distinct, non-empty value already used for this field anywhere in the
    // catalog — this is the picker's whole "known ids" pool. There's no separate
    // stored id list: an id simply stops showing up once no task references it
    // anymore, so there's nothing extra to keep in sync.
    private List<string> KnownValues(System.Func<TaskDefinition, string> selector) =>
        catalog.Definitions
            .Select(selector)
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .OrderBy(v => v)
            .ToList();

    // Dropdown of known ids + a "+" to type a brand-new one + a "×" to clear the
    // current value, instead of a free-text field that invites typos/duplicates.
    // fieldKey must be unique across every picker drawn in the same window (task
    // index + which field, or task/condition index + which field) — it's both the
    // inline-edit-mode identity and the GUI control name for autofocus.
    private void DrawIdField(string label, string fieldKey, SerializedProperty stringProp, List<string> knownIds)
    {
        var isEditingThis = newIdEditingKey == fieldKey;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(label);

            if (isEditingThis)
            {
                GUI.SetNextControlName($"NewId_{fieldKey}");
                newIdBuffer = EditorGUILayout.TextField(newIdBuffer);

                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    CommitNewId(stringProp);
                }

                if (GUILayout.Button("Cancel", GUILayout.Width(50)))
                {
                    newIdEditingKey = null;
                }

                return;
            }

            var current = stringProp.stringValue;

            if (GUILayout.Button(string.IsNullOrEmpty(current) ? "(none)" : current, EditorStyles.popup))
            {
                ShowIdMenu(stringProp, knownIds);
            }

            if (GUILayout.Button("+", GUILayout.Width(22)))
            {
                newIdEditingKey = fieldKey;
                newIdBuffer = "";
                // The text field doesn't exist until next repaint (isEditingThis is
                // still false for the rest of this frame); GUI.FocusControl holds
                // onto the name and focuses it as soon as it's drawn.
                GUI.FocusControl($"NewId_{fieldKey}");
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(current)))
            {
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    stringProp.stringValue = "";
                    stringProp.serializedObject.ApplyModifiedProperties();
                    TaskIdsCodeGenerator.Generate(catalog);
                }
            }
        }
    }

    private void ShowIdMenu(SerializedProperty stringProp, List<string> knownIds)
    {
        var menu = new GenericMenu();
        var current = stringProp.stringValue;

        if (knownIds.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No existing ids yet — use +"));
        }

        foreach (var id in knownIds)
        {
            var idCopy = id;
            menu.AddItem(new GUIContent(idCopy), idCopy == current, () => SelectId(stringProp, idCopy));
        }

        menu.ShowAsContext();
    }

    private void SelectId(SerializedProperty stringProp, string id)
    {
        stringProp.stringValue = id;
        stringProp.serializedObject.ApplyModifiedProperties();
        TaskIdsCodeGenerator.Generate(catalog);
        Repaint();
    }

    private void CommitNewId(SerializedProperty stringProp)
    {
        var trimmed = newIdBuffer.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            stringProp.stringValue = trimmed;
            stringProp.serializedObject.ApplyModifiedProperties();
            TaskIdsCodeGenerator.Generate(catalog);
        }

        newIdEditingKey = null;
        GUI.FocusControl(null);
    }

    private static string SafeFormat(string format, int value)
    {
        try
        {
            return string.Format(format, value);
        }
        catch
        {
            return "(invalid format — check for a stray { or })";
        }
    }

    private void DrawValidation()
    {
        var issues = new List<string>();
        var seenIds = new HashSet<string>();

        foreach (var task in catalog.Definitions)
        {
            if (string.IsNullOrEmpty(task.Id))
            {
                issues.Add("A task has an empty Id.");
            }
            else if (!seenIds.Add(task.Id))
            {
                issues.Add($"Duplicate Id \"{task.Id}\" — task lookups will only find the first one.");
            }

            if (string.IsNullOrEmpty(task.ProgressKey))
            {
                issues.Add($"\"{task.Id}\" has no ProgressKey — gameplay has nothing to report progress against.");
            }

            foreach (var condition in task.Conditions)
            {
                if (string.IsNullOrEmpty(condition.ParamKey))
                {
                    issues.Add($"\"{task.Id}\" has a condition with no Param set — it won't filter anything.");
                }
            }
        }

        if (issues.Count > 0)
        {
            EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
        }
    }
}
