using System;
using System.Collections.Generic;
using UnityEngine;

// Hidden host for AudioService: owns the voice pool and ticks every active voice once per
// frame. Created on first use and kept across scene loads, so no scene needs any setup.
internal sealed class AudioRunner : MonoBehaviour
{
    private readonly List<AudioVoice> active = new List<AudioVoice>();
    private readonly Stack<AudioVoice> free = new Stack<AudioVoice>();
    private int createdCount;
    private int maxVoices;

    public static AudioRunner Create(SoundCatalog catalog)
    {
        var go = new GameObject("[AudioService]");
        DontDestroyOnLoad(go);

        var runner = go.AddComponent<AudioRunner>();
        runner.maxVoices = Mathf.Max(1, catalog.maxVoices);
        var initial = Mathf.Clamp(catalog.initialVoices, 1, runner.maxVoices);
        for (var i = 0; i < initial; i++) runner.free.Push(runner.CreateVoice());
        return runner;
    }

    // Always returns a voice, already marked active: a free one, a new one while under the
    // cap, or else the oldest playing non-music sound, cut short.
    public AudioVoice Rent()
    {
        AudioVoice voice;
        if (free.Count > 0) voice = free.Pop();
        else if (createdCount < maxVoices) voice = CreateVoice();
        else voice = StealOldest();

        active.Add(voice);
        return voice;
    }

    public void ForEachActive(Action<AudioVoice> action)
    {
        for (var i = active.Count - 1; i >= 0; i--) action(active[i]);
    }

    private void Start()
    {
        // AudioMixer.SetFloat is ignored before the first Start, so re-apply saved volumes here.
        AudioService.ApplyVolumes();
    }

    private void LateUpdate()
    {
        // Unscaled so fades keep running while the game is paused with timeScale = 0.
        var deltaTime = Time.unscaledDeltaTime;
        for (var i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].Tick(deltaTime)) ReleaseAt(i);
        }
    }

    private AudioVoice CreateVoice() => new AudioVoice(transform, createdCount++);

    private AudioVoice StealOldest()
    {
        var oldest = -1;
        for (var i = 0; i < active.Count; i++)
        {
            var definition = active[i].Definition;
            if (definition != null && definition.channel == SoundChannel.Music) continue;
            if (oldest < 0 || active[i].StartTime < active[oldest].StartTime) oldest = i;
        }

        // Only music is playing: take the oldest track rather than fail.
        if (oldest < 0) oldest = 0;

        var voice = active[oldest];
        RemoveAt(oldest);
        voice.Release();
        return voice;
    }

    private void ReleaseAt(int index)
    {
        var voice = active[index];
        RemoveAt(index);
        voice.Release();
        free.Push(voice);
    }

    // Order of active voices doesn't matter, so swap-remove keeps this O(1).
    private void RemoveAt(int index)
    {
        var last = active.Count - 1;
        active[index] = active[last];
        active.RemoveAt(last);
    }
}
