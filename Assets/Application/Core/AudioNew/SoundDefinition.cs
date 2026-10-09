using System;
using UnityEngine;
using Random = UnityEngine.Random;

// One playable sound: a set of clip variations plus how to play them. Create via
// Assets > Create > Audio > Sound Definition and add it to the SoundCatalog.
[CreateAssetMenu(menuName = "Audio/Sound Definition", fileName = "Sound_")]
public class SoundDefinition : ScriptableObject
{
    [Tooltip("Lookup key used by SoundIds / AudioService. Leave empty to use the asset name.")]
    [SerializeField] private string id;

    public SoundChannel channel = SoundChannel.Sfx;

    [Tooltip("One is picked at random each play (never the same one twice in a row).")]
    public AudioClip[] clips = Array.Empty<AudioClip>();

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float volumeVariation;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.5f)] public float pitchVariation;
    public bool loop;

    [Tooltip("Minimum seconds between two plays of this sound; requests inside the window are dropped. 0 = no limit.")]
    [Min(0f)] public float cooldown;

    [Header("3D (only used by PlayAt / PlayAttached)")]
    public bool spatial;
    [Min(0f)] public float minDistance = 1f;
    [Min(0f)] public float maxDistance = 50f;

    [NonSerialized] private int lastClipIndex = -1;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id.Trim();
    public bool HasClips => clips != null && clips.Length > 0;

    public AudioClip PickClip()
    {
        if (!HasClips) return null;
        if (clips.Length == 1) return clips[0];

        // Shift past the previous pick so variations never repeat back to back.
        var hasPrevious = lastClipIndex >= 0;
        var index = Random.Range(0, hasPrevious ? clips.Length - 1 : clips.Length);
        if (hasPrevious && index >= lastClipIndex) index++;
        lastClipIndex = index;
        return clips[index];
    }

    public float RandomVolume() => Mathf.Clamp01(volume + Random.Range(-volumeVariation, volumeVariation));
    public float RandomPitch() => pitch + Random.Range(-pitchVariation, pitchVariation);
}
