using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// The single audio config asset: mixer routing, voice pool limits and every playable
// SoundDefinition. AudioService loads it from Resources/SoundCatalog on first use.
[CreateAssetMenu(menuName = "Audio/Sound Catalog", fileName = "SoundCatalog")]
public class SoundCatalog : ScriptableObject
{
    public const string ResourcesPath = "SoundCatalog";

    [Header("Mixer (optional - without it volumes are applied per AudioSource)")]
    public AudioMixer mixer;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup uiGroup;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup ambienceGroup;

    [Header("Voices")]
    [Tooltip("AudioSources created up front.")]
    [Min(1)] public int initialVoices = 8;
    [Tooltip("Hard cap. When reached, the oldest non-music sound is cut to make room.")]
    [Min(1)] public int maxVoices = 32;

    [Header("Music")]
    [Min(0f)] public float defaultMusicFade = 1f;

    [Header("Sounds")]
    public List<SoundDefinition> sounds = new List<SoundDefinition>();

    [NonSerialized] private Dictionary<string, SoundDefinition> lookup;

    public bool TryGet(string id, out SoundDefinition definition)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out definition);
    }

    public AudioMixerGroup GetGroup(SoundChannel channel) => channel switch
    {
        SoundChannel.UI => uiGroup,
        SoundChannel.Music => musicGroup,
        SoundChannel.Ambience => ambienceGroup,
        _ => sfxGroup
    };

    private void BuildLookup()
    {
        lookup = new Dictionary<string, SoundDefinition>(sounds.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var sound in sounds)
        {
            if (sound != null && !lookup.TryAdd(sound.Id, sound))
                Debug.LogWarning($"[SoundCatalog] Duplicate sound id \"{sound.Id}\" - only the first one is used.", this);
        }
    }

    private void OnValidate()
    {
        lookup = null;
        maxVoices = Mathf.Max(maxVoices, initialVoices);
    }
}
