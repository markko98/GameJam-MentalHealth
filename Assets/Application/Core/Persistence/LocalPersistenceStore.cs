using System;
using UnityEngine;

// PlayerPrefs-backed store. JsonUtility can't serialize a bare primitive or
// collection at the root, so values are wrapped before writing.
public class LocalPersistenceStore : IPersistenceStore
{
    [Serializable]
    private class Wrapper<T>
    {
        public T value;
    }

    public void Save<T>(string key, T value)
    {
        var json = JsonUtility.ToJson(new Wrapper<T> { value = value });
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        PersistenceKeyIndex.Track(key);
    }

    public T Load<T>(string key, T defaultValue = default)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;

        var wrapper = JsonUtility.FromJson<Wrapper<T>>(PlayerPrefs.GetString(key));
        return wrapper != null ? wrapper.value : defaultValue;
    }

    public bool HasKey(string key) => PlayerPrefs.HasKey(key);

    public void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PersistenceKeyIndex.Untrack(key);
    }

    public void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
        PersistenceKeyIndex.Clear();
    }
}
