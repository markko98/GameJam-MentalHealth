using System.Collections.Generic;
using UnityEngine;

// Static façade for all game audio, matching the rest of Core's services. A game fills the
// SoundCatalog (Resources/SoundCatalog) with SoundDefinitions; its Inspector regenerates
// SoundIds, so gameplay code writes:
//
//   AudioService.Play(SoundIds.Click);
//   AudioService.PlayAt(SoundIds.Splash, hitPoint);
//   var rain = AudioService.Play(SoundIds.Rain).FadeIn(2f);   ...   rain.Stop(2f);
//   AudioService.PlayMusic(SoundIds.MainTheme);
//
// Nothing needs to be in the scene - the first call creates everything. Volume/mute state
// lives in SettingsData (musicVolume/soundVolume/musicMuted/soundMuted), is persisted via
// SettingsService and broadcast through UEventBus<SoundSettingsChangedEvent>.
public static class AudioService
{
    // Exposed parameter names on the AudioMixer.
    private const string SfxParameter = "SFXVol";
    private const string UIParameter = "UIVol";
    private const string MusicParameter = "MusicVol";
    private const string AmbienceParameter = "AmbienceVol";

    private static SoundCatalog catalog;
    private static AudioRunner runner;
    private static bool isQuitting;

    private static AudioHandle music;
    private static SoundDefinition musicDefinition;

    private static readonly Dictionary<SoundDefinition, float> lastPlayTimes = new Dictionary<SoundDefinition, float>();
    private static readonly HashSet<string> reportedIds = new HashSet<string>();

    // Statics survive between Play Mode sessions when domain reload is disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        catalog = null;
        runner = null;
        isQuitting = false;
        music = default;
        musicDefinition = null;
        lastPlayTimes.Clear();
        reportedIds.Clear();
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    #region Setup

    /// <summary>Optional - every call initializes lazily from Resources/SoundCatalog. Call to use a different catalog.</summary>
    public static void Initialize(SoundCatalog soundCatalog = null)
    {
        catalog = soundCatalog != null ? soundCatalog : Resources.Load<SoundCatalog>(SoundCatalog.ResourcesPath);
        if (catalog == null)
        {
            Debug.LogError($"[AudioService] No SoundCatalog found at Resources/{SoundCatalog.ResourcesPath}. Audio is disabled.");
            catalog = ScriptableObject.CreateInstance<SoundCatalog>();
        }

        if (runner == null) runner = AudioRunner.Create(catalog);
        ApplyVolumes();
    }

    private static bool EnsureReady()
    {
        if (isQuitting) return false;
        if (runner == null) Initialize(catalog);
        return true;
    }

    private static void OnQuitting() => isQuitting = true;

    #endregion

    #region Playback

    /// <summary>Plays a sound with no position (2D). Use pitchScale for deliberate transposing, e.g. combo ladders.</summary>
    public static AudioHandle Play(SoundId id, float pitchScale = 1f) => PlayInternal(id, null, null, pitchScale);

    /// <summary>Plays a sound at a world position (3D if the definition is spatial).</summary>
    public static AudioHandle PlayAt(SoundId id, Vector3 position, float pitchScale = 1f) => PlayInternal(id, position, null, pitchScale);

    /// <summary>Plays a sound that follows a transform. Loops fade out by themselves if the target is destroyed.</summary>
    public static AudioHandle PlayAttached(SoundId id, Transform target, float pitchScale = 1f) => PlayInternal(id, null, target, pitchScale);

    /// <summary>Crossfades to a music track. Requesting the track that is already playing does nothing.</summary>
    public static void PlayMusic(SoundId id, float fade = -1f)
    {
        if (!EnsureReady() || !TryResolve(id, out var definition)) return;
        if (definition == musicDefinition && music.IsValid) return;

        fade = fade < 0f ? catalog.defaultMusicFade : fade;
        music.Stop(fade);
        musicDefinition = definition;
        music = PlayDefinition(definition, null, null, 1f).FadeIn(fade);
    }

    public static void StopMusic(float fade = -1f)
    {
        music.Stop(fade < 0f && catalog != null ? catalog.defaultMusicFade : fade);
        music = default;
        musicDefinition = null;
    }

    /// <summary>Freezes every sound in place (e.g. pause menu). Music keeps playing unless includeMusic is set.</summary>
    public static void PauseAll(bool includeMusic = false)
    {
        if (runner == null) return;
        runner.ForEachActive(voice =>
        {
            if (includeMusic || !IsMusic(voice)) voice.Pause();
        });
    }

    public static void ResumeAll()
    {
        if (runner == null) return;
        runner.ForEachActive(voice => voice.Resume());
    }

    /// <summary>Stops every sound (optionally fading). Music is only stopped when includeMusic is set.</summary>
    public static void StopAll(float fadeOut = 0f, bool includeMusic = false)
    {
        if (runner == null) return;
        if (includeMusic) StopMusic(fadeOut);
        runner.ForEachActive(voice =>
        {
            if (includeMusic || !IsMusic(voice)) voice.Stop(fadeOut);
        });
    }

    private static AudioHandle PlayInternal(SoundId id, Vector3? position, Transform target, float pitchScale)
    {
        if (!EnsureReady() || !TryResolve(id, out var definition)) return default;

        if (definition.cooldown > 0f)
        {
            var now = Time.unscaledTime;
            if (lastPlayTimes.TryGetValue(definition, out var last) && now - last < definition.cooldown) return default;
            lastPlayTimes[definition] = now;
        }

        return PlayDefinition(definition, position, target, pitchScale);
    }

    private static AudioHandle PlayDefinition(SoundDefinition definition, Vector3? position, Transform target, float pitchScale)
    {
        var clip = definition.PickClip();
        if (clip == null)
        {
            ReportOnce(definition.Id, $"[AudioService] \"{definition.Id}\" has an empty clip slot.");
            return default;
        }

        var voice = runner.Rent();
        voice.Play(definition, clip, definition.RandomVolume(), definition.RandomPitch() * pitchScale,
            ChannelGain(definition.channel), catalog.GetGroup(definition.channel), position, target);
        return new AudioHandle(voice);
    }

    private static bool TryResolve(SoundId id, out SoundDefinition definition)
    {
        definition = null;
        if (!id.IsValid) return false;

        if (!catalog.TryGet(id.Value, out definition))
        {
            ReportOnce(id.Value, $"[AudioService] No sound with id \"{id.Value}\" in the SoundCatalog.");
            return false;
        }

        if (!definition.HasClips)
        {
            ReportOnce(id.Value, $"[AudioService] \"{id.Value}\" has no AudioClips assigned.");
            return false;
        }

        return true;
    }

    // Missing sounds are usually requested every frame or every hit - warn once, not every time.
    private static void ReportOnce(string id, string message)
    {
        if (reportedIds.Add(id)) Debug.LogWarning(message);
    }

    private static bool IsMusic(AudioVoice voice) => voice.Definition != null && voice.Definition.channel == SoundChannel.Music;

    #endregion

    #region Volume settings

    public static bool MusicMuted => SettingsService.Current.musicMuted;
    public static bool SoundMuted => SettingsService.Current.soundMuted;
    public static float MusicVolume => SettingsService.Current.musicVolume;
    public static float SoundVolume => SettingsService.Current.soundVolume;

    public static void SetMusicMuted(bool muted)
    {
        SettingsService.Current.musicMuted = muted;
        OnSettingsChanged();
    }

    public static void SetSoundMuted(bool muted)
    {
        SettingsService.Current.soundMuted = muted;
        OnSettingsChanged();
    }

    public static void SetMusicVolume(float volume)
    {
        SettingsService.Current.musicVolume = Mathf.Clamp01(volume);
        OnSettingsChanged();
    }

    public static void SetSoundVolume(float volume)
    {
        SettingsService.Current.soundVolume = Mathf.Clamp01(volume);
        OnSettingsChanged();
    }

    private static void OnSettingsChanged()
    {
        SettingsService.Save();
        ApplyVolumes();
        UEventBus<SoundSettingsChangedEvent>.Raise(new SoundSettingsChangedEvent(MusicMuted, SoundMuted, MusicVolume, SoundVolume));
    }

    internal static void ApplyVolumes()
    {
        if (catalog == null) return;

        var mixer = catalog.mixer;
        if (mixer != null)
        {
            var musicDb = ToDecibel(EffectiveMusicVolume);
            var soundDb = ToDecibel(EffectiveSoundVolume);
            mixer.SetFloat(MusicParameter, musicDb);
            mixer.SetFloat(AmbienceParameter, musicDb);
            mixer.SetFloat(SfxParameter, soundDb);
            mixer.SetFloat(UIParameter, soundDb);
        }

        if (runner != null)
            runner.ForEachActive(voice => voice.SetGain(voice.Definition != null ? ChannelGain(voice.Definition.channel) : 1f));
    }

    // With a mixer the volume is applied on its groups; without one it is baked into each AudioSource.
    private static float ChannelGain(SoundChannel channel)
    {
        if (catalog.mixer != null) return 1f;
        return channel == SoundChannel.Music || channel == SoundChannel.Ambience ? EffectiveMusicVolume : EffectiveSoundVolume;
    }

    private static float EffectiveMusicVolume => MusicMuted ? 0f : MusicVolume;
    private static float EffectiveSoundVolume => SoundMuted ? 0f : SoundVolume;

    private static float ToDecibel(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;

    #endregion
}
