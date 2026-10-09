using UnityEngine;
using UnityEngine.Audio;

// One pooled AudioSource and its playback state. A plain class ticked by AudioRunner
// (no per-voice MonoBehaviour, coroutines or Invoke), so playing, fading and recycling
// a sound allocates nothing.
internal sealed class AudioVoice
{
    public readonly AudioSource Source;
    private readonly Transform transform;

    // Bumped every time the voice is recycled, which invalidates any AudioHandle still
    // pointing at its previous sound.
    public int Version { get; private set; }
    public SoundDefinition Definition { get; private set; }
    public float StartTime { get; private set; }
    public bool IsPaused { get; private set; }

    private float baseVolume;
    private float gain = 1f;
    private float fadeLevel = 1f;
    private float fadeTarget = 1f;
    private float fadeSpeed;
    private bool stopWhenFaded;

    private Transform follow;
    private bool isFollowing;

    public AudioVoice(Transform parent, int index)
    {
        var go = new GameObject($"Voice {index}");
        transform = go.transform;
        transform.SetParent(parent, false);

        Source = go.AddComponent<AudioSource>();
        Source.playOnAwake = false;
        Source.dopplerLevel = 0f;
    }

    public void Play(SoundDefinition definition, AudioClip clip, float volume, float pitch, float channelGain,
        AudioMixerGroup group, Vector3? position, Transform target)
    {
        Definition = definition;
        StartTime = Time.unscaledTime;
        baseVolume = volume;
        gain = channelGain;
        fadeLevel = fadeTarget = 1f;
        fadeSpeed = 0f;
        stopWhenFaded = false;
        IsPaused = false;

        follow = target;
        isFollowing = target != null;
        var positioned = isFollowing || position.HasValue;
        if (isFollowing) transform.position = target.position;
        else if (position.HasValue) transform.position = position.Value;

        Source.clip = clip;
        Source.outputAudioMixerGroup = group;
        Source.loop = definition.loop;
        Source.pitch = Mathf.Clamp(pitch, -3f, 3f);
        Source.spatialBlend = positioned && definition.spatial ? 1f : 0f;
        Source.minDistance = definition.minDistance;
        Source.maxDistance = definition.maxDistance;
        ApplyVolume();
        Source.Play();
    }

    // Returns true once the voice has finished and can go back to the pool.
    public bool Tick(float deltaTime)
    {
        if (IsPaused) return false;

        if (fadeSpeed > 0f)
        {
            fadeLevel = Mathf.MoveTowards(fadeLevel, fadeTarget, fadeSpeed * deltaTime);
            ApplyVolume();

            if (Mathf.Approximately(fadeLevel, fadeTarget))
            {
                fadeSpeed = 0f;
                if (stopWhenFaded) Source.Stop();
            }
        }

        if (isFollowing)
        {
            if (follow != null)
            {
                transform.position = follow.position;
            }
            else
            {
                // Target destroyed: one-shots finish where they are, loops would never end so fade them out.
                isFollowing = false;
                if (Source.loop) Stop(0.1f);
            }
        }

        return !Source.isPlaying;
    }

    public void FadeIn(float duration)
    {
        fadeLevel = 0f;
        ApplyVolume();
        FadeTo(1f, duration, false);
    }

    public void Stop(float fadeOut)
    {
        if (fadeOut > 0f && !IsPaused)
        {
            FadeTo(0f, fadeOut, true);
            return;
        }

        IsPaused = false;
        Source.Stop();
    }

    public void Pause()
    {
        if (IsPaused || !Source.isPlaying) return;
        IsPaused = true;
        Source.Pause();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Source.UnPause();
    }

    public void SetGain(float channelGain)
    {
        gain = channelGain;
        ApplyVolume();
    }

    public void Release()
    {
        Source.Stop();
        Source.clip = null;
        Definition = null;
        follow = null;
        isFollowing = false;
        IsPaused = false;
        Version++;
    }

    private void FadeTo(float target, float duration, bool stopAfter)
    {
        fadeTarget = target;
        stopWhenFaded = stopAfter;

        if (duration <= 0f)
        {
            fadeLevel = target;
            fadeSpeed = 0f;
            ApplyVolume();
            if (stopAfter) Source.Stop();
            return;
        }

        fadeSpeed = Mathf.Abs(target - fadeLevel) / duration;
        if (fadeSpeed <= 0f && stopAfter) Source.Stop();
    }

    private void ApplyVolume() => Source.volume = baseVolume * fadeLevel * gain;
}
