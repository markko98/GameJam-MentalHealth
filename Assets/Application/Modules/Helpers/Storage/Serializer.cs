using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

/// Saves are written as JSON (Newtonsoft, already in the project) instead of BinaryFormatter. BinaryFormatter
/// payloads embed assembly and type names, so renaming a class, moving it between assemblies or upgrading Unity's
/// scripting backend silently invalidated every existing save.
public static class Serializer
{
    // Payloads written here carry a prefix so the loader can tell a JSON blob from a legacy base64 one.
    private const string JsonPrefix = "json1:";

    private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        // BinaryFormatter serialized *fields*, public and private alike.
        ContractResolver = new FieldsContractResolver(),
        // Field initializers already run in the constructor.
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
        // Enums are written as names, not ordinals.
        Converters = { new StringEnumConverter() },
    };

    /*
     * Why StringEnumConverter is not optional here.
     *
     * Newtonsoft writes enums as integers by default, and the game's enums declare no explicit values:
     * PlaneType, ChapterType, EnvironmentModelType, CurrencyType and the rest are positional. So inserting
     * a plane into the middle of PlaneType — an ordinary thing to want to do — silently repoints every
     * value already on disk. LocalPersistentStorageManager persists List<PlaneType> for unlocked planes
     * and List<PlaneLevelEntry> for levels, so a player who had unlocked the Spitfire would afterwards own
     * a different aircraft, at a level they never earned. Nothing reports it, because a 12 is a perfectly
     * valid 12; only its meaning moved. That is the failure this prevents.
     *
     * Reading is unaffected by the switch: StringEnumConverter accepts an integer token as well as a name,
     * so saves written before this change still load, and each key is rewritten as names the next time it
     * is saved. There is no separate migration pass and no version bump — the format converges on its own.
     *
     * Dictionary keys were never exposed: Newtonsoft writes a non-string key via ToString(), so
     * Dictionary<CurrencyType, int> was already keyed by name.
     */

    public static void SaveToPlayerPrefs<T>(string key, T data)
    {
        if (data == null)
        {
            Debug.Log("Data is null for key:" + key);
            return;
        }

        try
        {
            PlayerPrefs.SetString(key, JsonPrefix + JsonConvert.SerializeObject(data, jsonSettings));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Serializer] Failed to serialize '{key}': {e.Message}");
        }
    }

    public static T LoadFromPlayerPrefs<T>(string key)
    {
        if (!PlayerPrefs.HasKey(key)) return default(T);

        string raw = PlayerPrefs.GetString(key);

        if (string.IsNullOrEmpty(raw))
        {
            // The key exists but is not a string: it was written straight through PlayerPrefs as an int/float/string.
            if (!TryReadPrimitive<T>(key, out var primitive))
            {
                // Should never happen
                Debug.Log("Attempted to deserialize a non primitive type for key:" + key);
                return default(T);
            }

            SaveToPlayerPrefs(key, primitive);
            return primitive;
        }

        if (raw.StartsWith(JsonPrefix, StringComparison.Ordinal))
        {
            return FromJson<T>(raw.Substring(JsonPrefix.Length), key);
        }

        // Legacy BinaryFormatter blob. Read it once, rewrite it as JSON, never come back down this path.
        if (TryReadLegacyBinary<T>(raw, key, out var migrated) && migrated != null)
        {
            SaveToPlayerPrefs(key, migrated);
            return migrated;
        }

        return default(T);
    }

    public static void AutoInitializeCollections<T>(T obj)
    {
        if (obj == null) return;

        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var value = field.GetValue(obj);
            if (value != null) continue;

            var fieldType = field.FieldType;

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                var newInstance = Activator.CreateInstance(fieldType);
                field.SetValue(obj, newInstance);
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var newInstance = Activator.CreateInstance(fieldType);
                field.SetValue(obj, newInstance);
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var newInstance = Activator.CreateInstance(fieldType);
                field.SetValue(obj, newInstance);
            }
            else if (fieldType == typeof(string))
            {
                field.SetValue(obj, string.Empty);
            }
        }
    }

    private static T FromJson<T>(string json, string key)
    {
        try
        {
            T result = JsonConvert.DeserializeObject<T>(json, jsonSettings);
            AutoInitializeCollections(result);
            return result;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Serializer] Could not read save '{key}': {e.Message}");
            return default(T);
        }
    }

    private static bool TryReadLegacyBinary<T>(string base64, string key, out T value)
    {
        value = default(T);

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                object obj = formatter.Deserialize(stream);
                if (!(obj is T typed)) return false;

                AutoInitializeCollections(typed);
                value = typed;
                return true;
            }
        }
        catch (Exception e)
        {
            // Written by a build whose type names no longer match, or simply corrupt.
            Debug.LogWarning($"[Serializer] Could not migrate legacy save '{key}': {e.Message}");
            return false;
        }
    }

    private static bool TryReadPrimitive<T>(string key, out T value)
    {
        if (typeof(T) == typeof(int))
        {
            value = (T)(object)PlayerPrefs.GetInt(key);
            return true;
        }

        if (typeof(T) == typeof(float))
        {
            value = (T)(object)PlayerPrefs.GetFloat(key);
            return true;
        }

        if (typeof(T) == typeof(string))
        {
            value = (T)(object)PlayerPrefs.GetString(key);
            return true;
        }

        value = default(T);
        return false;
    }

    /// <summary>Serializes instance fields and ignores properties, mirroring what <see cref="BinaryFormatter"/> used to persist.</summary>
    private sealed class FieldsContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var members = new List<MemberInfo>();

            for (var t = objectType; t != null && t != typeof(object); t = t.BaseType)
            {
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                {
                    if (field.IsNotSerialized) continue;
                    members.Add(field);
                }
            }

            return members;
        }

        /// <summary>Listing a private field in <see cref="GetSerializableMembers"/> is not enough.</summary>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, MemberSerialization.Fields);
            property.Readable = true;
            property.Writable = true;
            return property;
        }
    }
}
