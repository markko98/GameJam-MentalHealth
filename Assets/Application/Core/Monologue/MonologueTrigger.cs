using UnityEngine;

// Plays a monologue from the Inspector: on start, on trigger enter or from a UnityEvent.
public class MonologueTrigger : MonoBehaviour
{
    [Tooltip("The monologue to play. Leave empty to use the id below (catalogue lookup).")]
    [SerializeField] private MonologueDefinition monologue;
    [Tooltip("Used when no definition is assigned.")]
    [SerializeField] private MonologueId monologueId;

    [Header("When")]
    [SerializeField] private bool playOnStart;
    [Tooltip("Needs a Collider2D/Collider with isTrigger on this object.")]
    [SerializeField] private bool playOnTriggerEnter;
    [Tooltip("Only objects with this tag trigger it. Empty = anything.")]
    [SerializeField] private string requiredTag = "Player";
    [SerializeField, Min(0f)] private float delay;
    [Tooltip("Fires at most once, however it is triggered.")]
    [SerializeField] private bool onlyOnce = true;

    private bool hasPlayed;

    public void Play()
    {
        if (onlyOnce && hasPlayed) return;
        hasPlayed = true;

        if (delay > 0f) Invoke(nameof(PlayNow), delay);
        else PlayNow();
    }

    private void PlayNow()
    {
        if (monologue != null) MonologueService.Play(monologue);
        else MonologueService.Play(monologueId);
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playOnTriggerEnter && Matches(other.tag)) Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnTriggerEnter && Matches(other.tag)) Play();
    }

    private bool Matches(string otherTag) => string.IsNullOrEmpty(requiredTag) || otherTag == requiredTag;
}
