// Safe reference to one playing sound. Voices are pooled, so once the sound ends the handle
// simply goes invalid and every call becomes a no-op - it can never touch a recycled voice.
public readonly struct AudioHandle
{
    private readonly AudioVoice voice;
    private readonly int version;

    internal AudioHandle(AudioVoice voice)
    {
        this.voice = voice;
        version = voice.Version;
    }

    // Source check covers app quit / runner teardown, when the AudioSource is already destroyed.
    public bool IsValid => voice != null && voice.Version == version && voice.Source != null;
    public bool IsPaused => IsValid && voice.IsPaused;

    /// <summary>Starts the sound silent and fades it up. Chainable: <c>AudioService.Play(id).FadeIn(1f)</c>.</summary>
    public AudioHandle FadeIn(float duration)
    {
        if (IsValid) voice.FadeIn(duration);
        return this;
    }

    public void Stop(float fadeOut = 0f)
    {
        if (IsValid) voice.Stop(fadeOut);
    }

    public void Pause()
    {
        if (IsValid) voice.Pause();
    }

    public void Resume()
    {
        if (IsValid) voice.Resume();
    }
}
