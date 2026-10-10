using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Plays lines on a UILabel: typewriter, per-line sound, reading time, fade in/out.
public class UISubtitles : MonoBehaviour
{
    [SerializeField] private UILabel label;
    [Tooltip("Plays each line's AudioClip (voice-over). Catalogue sounds go through AudioService " +
             "instead. Route it to a mixer group so the volume settings apply.")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private bool typewriter = true;
    [Tooltip("Go to the next line by itself. Off = waits for Next().")]
    [SerializeField] private bool autoAdvance = true;
    [Tooltip("Minimum time a line stays on screen after it is fully shown.")]
    [SerializeField, Min(0f)] private float minLineDuration = 1.5f;
    [Tooltip("Reading time added per character.")]
    [SerializeField, Min(0f)] private float secondsPerCharacter = 0.04f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [Tooltip("Hold a line until its voice clip has finished, even past its reading time.")]
    [SerializeField] private bool waitForVoice = true;
    [Tooltip("Fade-out of a line's catalogue sound when the line ends or is skipped.")]
    [SerializeField, Min(0f)] private float soundFadeOut = 0.15f;

    private struct Settings
    {
        public bool typewriter;
        public bool autoAdvance;
        public bool waitForVoice;
        public float minLineDuration;
        public float secondsPerCharacter;
        public float fadeDuration;
        public float charactersPerSecond;
    }

    private readonly Queue<MonologueLine> queue = new Queue<MonologueLine>();
    private Coroutine routine;
    private Action finished;
    private bool advanceRequested;
    private Settings defaults;
    private bool defaultsCaptured;
    private float charactersPerSecondOverride;
    private AudioHandle lineSound;

    public event Action<MonologueLine> LineStarted;

    public UILabel Label => label != null ? label : label = GetComponent<UILabel>();
    public bool IsPlaying => routine != null;
    public bool IsTyping => Label.IsTyping;
    public bool IsWaitingForNext { get; private set; }

    public bool AutoAdvance { get => autoAdvance; set => autoAdvance = value; }
    public bool WaitForVoice { get => waitForVoice; set => waitForVoice = value; }
    public float MinLineDuration { get => minLineDuration; set => minLineDuration = Mathf.Max(0f, value); }
    public float SecondsPerCharacter { get => secondsPerCharacter; set => secondsPerCharacter = Mathf.Max(0f, value); }
    public float FadeDuration { get => fadeDuration; set => fadeDuration = Mathf.Max(0f, value); }
    public float CharactersPerSecond
    {
        get => Label.CharactersPerSecond;
        set
        {
            CaptureDefaults();
            charactersPerSecondOverride = value;
            Label.CharactersPerSecond = value;
        }
    }

    public void Show(string line, float duration = -1f) => Show(new MonologueLine(line, Mathf.Max(0f, duration)));

    public void Show(MonologueLine line)
    {
        if (line == null || !line.HasText) return;

        queue.Enqueue(line);
        if (routine == null && isActiveAndEnabled) routine = StartCoroutine(Run());
    }

    public void Play(IEnumerable<string> lines, Action onFinished = null)
    {
        Clear();
        finished = onFinished;
        foreach (var line in lines) Show(line);
        if (routine == null) EndRun();
    }

    public void Play(IEnumerable<MonologueLine> lines, Action onFinished = null)
    {
        Clear();
        finished = onFinished;
        foreach (var line in lines) Show(line);

        if (routine == null) EndRun();
    }

    public void Next()
    {
        if (Label.IsTyping) Label.CompleteTyping();
        else advanceRequested = true;
    }

    public void Finish()
    {
        if (routine == null) return;

        StopCoroutine(routine);
        routine = null;
        queue.Clear();
        EndRun();
    }

    public void Clear()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        queue.Clear();
        finished = null;
        IsWaitingForNext = false;
        StopVoice();
        if (label != null) label.Clear();
    }

    public void ResetSettings()
    {
        CaptureDefaults();
        typewriter = defaults.typewriter;
        autoAdvance = defaults.autoAdvance;
        waitForVoice = defaults.waitForVoice;
        minLineDuration = defaults.minLineDuration;
        secondsPerCharacter = defaults.secondsPerCharacter;
        fadeDuration = defaults.fadeDuration;
        charactersPerSecondOverride = 0f;
        Label.CharactersPerSecond = defaults.charactersPerSecond;
    }

    private void Awake()
    {
        if (label == null) label = GetComponent<UILabel>();
        CaptureDefaults();
    }

    private void OnDisable() => Clear();

    private void CaptureDefaults()
    {
        if (defaultsCaptured) return;
        defaultsCaptured = true;

        defaults = new Settings
        {
            typewriter = typewriter,
            autoAdvance = autoAdvance,
            waitForVoice = waitForVoice,
            minLineDuration = minLineDuration,
            secondsPerCharacter = secondsPerCharacter,
            fadeDuration = fadeDuration,
            charactersPerSecond = Label.CharactersPerSecond
        };
    }

    private IEnumerator Run()
    {
        while (queue.Count > 0)
        {
            var line = queue.Dequeue();
            var text = line.text ?? string.Empty;

            advanceRequested = false;
            IsWaitingForNext = false;

            Label.CharactersPerSecond = line.charactersPerSecond > 0f ? line.charactersPerSecond : BaseCharactersPerSecond;
            PlayLineSound(line);
            LineStarted?.Invoke(line);

            if (typewriter) Label.Type(text);
            else Label.Text = text;
            if (fadeDuration > 0f) Label.FadeIn(fadeDuration);
            else Label.Alpha = 1f;

            while (Label.IsTyping) yield return null;

            var hold = line.holdDuration > 0f ? line.holdDuration : Mathf.Max(minLineDuration, text.Length * secondsPerCharacter);
            IsWaitingForNext = !autoAdvance;

            for (var time = 0f; !advanceRequested && (!autoAdvance || time < hold || IsVoicePlaying); time += Time.unscaledDeltaTime)
                yield return null;

            IsWaitingForNext = false;
            StopVoice();

            if (fadeDuration > 0f) yield return Label.FadeOut(fadeDuration).WaitForCompletion();
        }

        routine = null;
        EndRun();
    }

    private float BaseCharactersPerSecond => charactersPerSecondOverride > 0f ? charactersPerSecondOverride : defaults.charactersPerSecond;

    private bool IsVoicePlaying => waitForVoice && ((voiceSource != null && voiceSource.isPlaying) || lineSound.IsValid);

    private void PlayLineSound(MonologueLine line)
    {
        if (line.sound.IsValid) lineSound = AudioService.Play(line.sound);
        if (line.clip == null) return;

        if (voiceSource == null)
        {
            Debug.LogWarning($"[UISubtitles] A line has a voice clip but no AudioSource is assigned on {name}.", this);
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = line.clip;
        voiceSource.Play();
    }

    // Both the voice clip and the catalogue sound belong to the line, so they end with it.
    private void StopVoice()
    {
        if (voiceSource != null && voiceSource.isPlaying) voiceSource.Stop();
        lineSound.Stop(soundFadeOut);
        lineSound = default;
    }

    private void EndRun()
    {
        IsWaitingForNext = false;
        StopVoice();
        Label.Clear();

        var completed = finished;
        finished = null;
        completed?.Invoke();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        label = GetComponent<UILabel>();
        voiceSource = GetComponentInParent<AudioSource>();
    }
#endif
}
