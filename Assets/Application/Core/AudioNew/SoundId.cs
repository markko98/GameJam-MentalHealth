using System;
using UnityEngine;

// A SoundCatalog lookup key, typed instead of a raw string. SoundIds (Generated/SoundIds.cs)
// exposes every catalog entry as a const string; the implicit conversions let callers pass
// those consts straight into AudioService. Lookups are case-insensitive.
[Serializable]
public struct SoundId : IEquatable<SoundId>
{
    [SerializeField] private string value;

    public SoundId(string id)
    {
        value = id?.Trim();
    }

    public string Value => value ?? string.Empty;
    public bool IsValid => !string.IsNullOrEmpty(value);

    public override string ToString() => Value;
    public bool Equals(SoundId other) => StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);
    public override bool Equals(object obj) => obj is SoundId other && Equals(other);
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public static implicit operator string(SoundId id) => id.Value;
    public static implicit operator SoundId(string id) => new SoundId(id);
}
