using System;
using UnityEngine;

// MonologueCatalogue lookup key, like SoundId. Consts live in Generated/MonologueIds.cs.
[Serializable]
public struct MonologueId : IEquatable<MonologueId>
{
    [SerializeField] private string value;

    public MonologueId(string id)
    {
        value = id?.Trim();
    }

    public string Value => value ?? string.Empty;
    public bool IsValid => !string.IsNullOrEmpty(value);

    public override string ToString() => Value;
    public bool Equals(MonologueId other) => StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
    public override bool Equals(object obj) => obj is MonologueId other && Equals(other);
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public static implicit operator string(MonologueId id) => id.Value;
    public static implicit operator MonologueId(string id) => new MonologueId(id);
}
