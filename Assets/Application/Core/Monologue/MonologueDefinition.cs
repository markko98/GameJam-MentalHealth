using System.Collections.Generic;
using UnityEngine;

// One monologue. Create via Assets > Create > Monologue > Monologue. 0 = use the view's value.
[CreateAssetMenu(menuName = "Monologue/Monologue", fileName = "Monologue_")]
public class MonologueDefinition : ScriptableObject
{
    [Tooltip("Lookup key used by MonologueIds / MonologueService. Leave empty to use the asset name.")]
    [SerializeField] private string id;

    [Header("Speaker (per-line values win)")]
    public string speaker;
    public Sprite portrait;

    [Header("Lines")]
    public List<MonologueLine> lines = new List<MonologueLine>();

    [Header("Pacing (0 = the view prefab's value)")]
    [Tooltip("Typing speed, in characters per second.")]
    [Min(0f)] public float charactersPerSecond;

    [Tooltip("Shortest time a finished line stays on screen.")]
    [Min(0f)] public float minLineDuration;

    [Tooltip("Reading time added per character of the line.")]
    [Min(0f)] public float secondsPerCharacter;

    [Tooltip("Cross-fade between two lines.")]
    [Min(0f)] public float lineFadeDuration;

    [Header("Flow")]
    [Tooltip("On = lines advance by themselves. Off = every line waits for a tap / MonologueService.Next().")]
    public bool autoAdvance = true;

    [Tooltip("A tap finishes the typewriter instantly, a second one moves on. Off = the player cannot rush it.")]
    public bool skippable = true;

    [Tooltip("Keep a line on screen until its voice clip has finished, even past its reading time.")]
    public bool waitForVoice = true;

    [Tooltip("Plays at most once per session - intros, hints, one-off thoughts. " +
             "MonologueService.ClearHistory() forgets it again.")]
    public bool playOnce;

    [Header("Presentation")]
    [Tooltip("Shown in this prefab instead of the catalogue's default view. Needs a MonologueView on its root.")]
    public GameObject viewPrefab;

    [Tooltip("Played as the box pops in.")]
    public SoundId openSound;

    [Tooltip("Played as the box pops out.")]
    public SoundId closeSound;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id.Trim();

    public bool HasLines
    {
        get
        {
            if (lines == null) return false;
            foreach (var line in lines)
            {
                if (line != null && line.HasText) return true;
            }

            return false;
        }
    }
}
