using System;
using UnityEngine;

// One line of a MonologueDefinition.
[Serializable]
public class MonologueLine
{
    [TextArea(2, 6)] public string text;

    [Tooltip("Name shown above the text. Empty = the monologue's speaker.")]
    public string speaker;

    [Tooltip("Portrait shown next to the text. None = the monologue's portrait.")]
    public Sprite portrait;

    [Header("Sound")]
    [Tooltip("Voice-over clip played when the line starts, through the view's own AudioSource. " +
             "It is stopped when the line is over, so lines never talk over each other.")]
    public AudioClip clip;

    [Tooltip("SoundCatalog sound played when the line starts - for sounds that want a mixer " +
             "channel, clip variations or a cooldown (sighs, stings, UI blips).")]
    public SoundId sound;

    [Header("Timing (0 = the monologue's value)")]
    [Tooltip("Typing speed for this line, in characters per second.")]
    [Min(0f)] public float charactersPerSecond;

    [Tooltip("Seconds the finished line stays on screen. 0 = calculated from its length.")]
    [Min(0f)] public float holdDuration;

    public MonologueLine() { }

    public MonologueLine(string text, float holdDuration = 0f)
    {
        this.text = text;
        this.holdDuration = holdDuration;
    }

    public bool HasText => !string.IsNullOrWhiteSpace(text);
}
