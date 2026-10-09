using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Shows lines one after another on a UILabel: typewriter, reading time, fade in/out.
// With autoAdvance off it waits for Next(), which makes it a click-to-continue dialogue box.
public class UISubtitles : MonoBehaviour
{
    [SerializeField] private UILabel label;
    [SerializeField] private bool typewriter = true;
    [Tooltip("Go to the next line by itself. Off = waits for Next().")]
    [SerializeField] private bool autoAdvance = true;
    [Tooltip("Minimum time a line stays on screen after it is fully shown.")]
    [SerializeField, Min(0f)] private float minLineDuration = 1.5f;
    [Tooltip("Reading time added per character.")]
    [SerializeField, Min(0f)] private float secondsPerCharacter = 0.04f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    private readonly Queue<(string text, float duration)> queue = new Queue<(string, float)>();
    private Coroutine routine;
    private Action finished;
    private bool advanceRequested;

    public bool IsPlaying => routine != null;

    // Queues a line; duration < 0 means it is calculated from the text length.
    public void Show(string line, float duration = -1f)
    {
        queue.Enqueue((line, duration));
        if (routine == null && isActiveAndEnabled) routine = StartCoroutine(Run());
    }

    // Replaces whatever is playing with these lines.
    public void Play(IEnumerable<string> lines, Action onFinished = null)
    {
        Clear();
        finished = onFinished;
        foreach (var line in lines) Show(line);
    }

    // Finishes the typewriter first; a second call moves on to the next line.
    public void Next()
    {
        if (label.IsTyping) label.CompleteTyping();
        else advanceRequested = true;
    }

    public void Clear()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        queue.Clear();
        finished = null;
        if (label != null) label.Clear();
    }

    private void Awake()
    {
        if (label == null) label = GetComponent<UILabel>();
    }

    private void OnDisable() => Clear();

    private IEnumerator Run()
    {
        while (queue.Count > 0)
        {
            var (text, duration) = queue.Dequeue();
            advanceRequested = false;

            if (typewriter) label.Type(text);
            else label.Text = text;
            if (fadeDuration > 0f) label.FadeIn(fadeDuration);
            else label.Alpha = 1f;

            while (label.IsTyping) yield return null;

            var hold = duration >= 0f ? duration : Mathf.Max(minLineDuration, text.Length * secondsPerCharacter);
            for (var time = 0f; !advanceRequested && (!autoAdvance || time < hold); time += Time.unscaledDeltaTime)
                yield return null;

            if (fadeDuration > 0f) yield return label.FadeOut(fadeDuration).WaitForCompletion();
        }

        label.Clear();
        routine = null;

        var completed = finished;
        finished = null;
        completed?.Invoke();
    }

#if UNITY_EDITOR
    private void Reset() => label = GetComponent<UILabel>();
#endif
}
