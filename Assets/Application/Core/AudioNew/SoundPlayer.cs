using UnityEngine;

// Plays a catalog sound from the Inspector without code: on Start/OnEnable, or from a
// UnityEvent (e.g. Button.onClick -> SoundPlayer.Play).
public class SoundPlayer : MonoBehaviour
{
    public enum Trigger { Manual, OnStart, OnEnable }
    public enum Placement { Global, AtThisPosition, AttachedToThis }

    [SerializeField] private SoundId sound;
    [SerializeField] private Trigger playOn = Trigger.Manual;
    [SerializeField] private Placement placement = Placement.Global;
    [SerializeField, Min(0f)] private float fadeIn;

    [Tooltip("Stop the sound when this object is disabled or destroyed - mainly for loops.")]
    [SerializeField] private bool stopOnDisable;
    [SerializeField, Min(0f)] private float fadeOut;

    private AudioHandle handle;

    private void Start()
    {
        if (playOn == Trigger.OnStart) Play();
    }

    private void OnEnable()
    {
        if (playOn == Trigger.OnEnable) Play();
    }

    private void OnDisable()
    {
        if (stopOnDisable) Stop();
    }

    public void Play()
    {
        handle = placement switch
        {
            Placement.AtThisPosition => AudioService.PlayAt(sound, transform.position),
            Placement.AttachedToThis => AudioService.PlayAttached(sound, transform),
            _ => AudioService.Play(sound)
        };

        if (fadeIn > 0f) handle.FadeIn(fadeIn);
    }

    public void Stop() => handle.Stop(fadeOut);
}
