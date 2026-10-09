using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Listener list shared by the UI components. A listener whose owner (explicit, or the
// MonoBehaviour the method belongs to) has been destroyed is dropped automatically, and an
// exception in one listener is logged without stopping the others.
public sealed class UIListeners<T>
{
    private readonly struct Entry
    {
        public readonly Delegate Key;
        public readonly Action<T> Invoke;
        public readonly Object Owner;
        public readonly bool HasOwner;

        public Entry(Delegate key, Action<T> invoke, Object owner)
        {
            Key = key;
            Invoke = invoke;
            Owner = owner;
            HasOwner = !ReferenceEquals(owner, null);
        }

        public bool IsDead => HasOwner && Owner == null;
    }

    private readonly List<Entry> entries = new List<Entry>();

    public int Count => entries.Count;

    public void Add(Delegate key, Action<T> invoke, Object owner = null)
    {
        if (key == null) return;

        Remove(key);
        entries.Add(new Entry(key, invoke, ReferenceEquals(owner, null) ? key.Target as Object : owner));
    }

    public void Remove(Delegate key) => entries.RemoveAll(entry => entry.Key.Equals(key));

    public void Clear() => entries.Clear();

    public void Invoke(T value)
    {
        if (entries.Count == 0) return;

        entries.RemoveAll(entry => entry.IsDead);
        foreach (var entry in entries.ToArray())
        {
            if (entry.IsDead) continue;

            try
            {
                entry.Invoke(value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, entry.Owner);
            }
        }
    }
}
