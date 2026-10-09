using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Lists every key LocalPersistenceStore has saved (tracked via PersistenceKeyIndex,
// since PlayerPrefs itself has no enumeration API), so local save data can be
// inspected/cleared without digging through the registry/plist by hand. Only shows
// keys saved through PersistenceService — not ones set directly via PlayerPrefs.SetX()
// elsewhere or by third-party SDKs.
public class PlayerPrefsWindow : EditorWindow
{
    private Vector2 scroll;
    private string search = "";

    [MenuItem("Tools/PlayerPrefs Viewer")]
    public static void Open()
    {
        GetWindow<PlayerPrefsWindow>("PlayerPrefs");
    }

    private void OnGUI()
    {
        DrawToolbar();

        var keys = PersistenceKeyIndex.GetKeysSorted();
        if (!string.IsNullOrEmpty(search))
        {
            keys = keys.Where(k => k.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        if (keys.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No tracked PlayerPrefs keys yet. Keys show up here once something saves " +
                "through PersistenceService (e.g. SettingsService.Save()).",
                MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var key in keys)
        {
            DrawRow(key);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Search", GUILayout.Width(50));
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Repaint();
            }

            if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Delete All PlayerPrefs",
                    "This clears every PlayerPrefs key on this machine, including any not " +
                    "saved through PersistenceService. Continue?",
                    "Delete All", "Cancel"))
                {
                    PlayerPrefs.DeleteAll();
                    PersistenceKeyIndex.Clear();
                }
            }
        }
    }

    private void DrawRow(string key)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(key, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    PlayerPrefs.DeleteKey(key);
                    PersistenceKeyIndex.Untrack(key);
                    return;
                }
            }

            if (!PlayerPrefs.HasKey(key))
            {
                EditorGUILayout.LabelField("(no value — stale index entry)", EditorStyles.miniLabel);
                return;
            }

            var pretty = FormatJson(PlayerPrefs.GetString(key));
            var lineCount = pretty.Count(c => c == '\n') + 1;
            EditorGUILayout.SelectableLabel(pretty, EditorStyles.textArea, GUILayout.Height(lineCount * 14f + 6f));
        }
    }

    // LocalPersistenceStore writes JsonUtility's compact single-line output, and
    // there's no JSON library in this project to lean on for pretty-printing — so
    // this walks the raw string directly, indenting on { [ and breaking on , the
    // same way any formatter would, while leaving string contents untouched.
    private static string FormatJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        var sb = new StringBuilder();
        var depth = 0;
        var inString = false;
        var escapeNext = false;

        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];

            if (inString)
            {
                sb.Append(c);
                if (escapeNext) escapeNext = false;
                else if (c == '\\') escapeNext = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    sb.Append(c);
                    break;
                case '{':
                case '[':
                    var closing = c == '{' ? '}' : ']';
                    var isEmpty = i + 1 < json.Length && json[i + 1] == closing;
                    sb.Append(c);
                    if (!isEmpty)
                    {
                        depth++;
                        sb.Append('\n').Append(' ', depth * 2);
                    }
                    break;
                case '}':
                case ']':
                    var wasEmpty = sb.Length > 0 && (sb[sb.Length - 1] == '{' || sb[sb.Length - 1] == '[');
                    if (!wasEmpty)
                    {
                        depth--;
                        sb.Append('\n').Append(' ', depth * 2);
                    }
                    sb.Append(c);
                    break;
                case ',':
                    sb.Append(c).Append('\n').Append(' ', depth * 2);
                    break;
                case ':':
                    sb.Append(": ");
                    break;
                default:
                    if (!char.IsWhiteSpace(c)) sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
