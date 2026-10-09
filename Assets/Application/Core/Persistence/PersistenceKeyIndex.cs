using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// LocalPersistenceStore is PlayerPrefs-backed, and PlayerPrefs has no API to
// enumerate its own keys. So every key it writes gets recorded here (itself
// stored via PlayerPrefs) purely so tooling — e.g. the PlayerPrefs debug
// window — can list what's been saved without depending on undocumented,
// platform-specific PlayerPrefs storage internals (Windows registry layout,
// plist format, etc).
public static class PersistenceKeyIndex
{
    private const string IndexKey = "__persistence_key_index__";
    private const char Separator = '|';

    public static void Track(string key)
    {
        var keys = GetKeys();
        if (!keys.Add(key)) return;

        PlayerPrefs.SetString(IndexKey, string.Join(Separator, keys));
        PlayerPrefs.Save();
    }

    public static void Untrack(string key)
    {
        var keys = GetKeys();
        if (!keys.Remove(key)) return;

        PlayerPrefs.SetString(IndexKey, string.Join(Separator, keys));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(IndexKey);
    }

    public static List<string> GetKeysSorted()
    {
        var keys = GetKeys().ToList();
        keys.Sort();
        return keys;
    }

    private static HashSet<string> GetKeys()
    {
        if (!PlayerPrefs.HasKey(IndexKey)) return new HashSet<string>();

        return PlayerPrefs.GetString(IndexKey)
            .Split(Separator)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet();
    }
}
