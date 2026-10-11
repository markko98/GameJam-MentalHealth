using System;
using UnityEngine;

[Flags]
public enum InteractionGroup
{
    None = 0,
    Desktop = 1 << 0,
    Items = 1 << 1,
    Window = 1 << 2,
    All = ~0
}

// Put this on anything that should only take clicks in some modes - the desktop UI in Task, the
// cat and the photos in Break. Outside its modes, or when none of its groups are allowed
// (GameModeInteractable.Allow), the object ignores the pointer and clicks fall through.
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class GameModeInteractable : MonoBehaviour
{
    public static InteractionGroup Allowed { get; private set; } = InteractionGroup.All;
    private static event Action AllowedChanged;

    [Tooltip("Modes in which this object can be clicked.")]
    [SerializeField] private GameMode[] activeIn = { GameMode.Break };
    [SerializeField] private InteractionGroup group = InteractionGroup.Items;

    private CanvasGroup canvasGroup;
    private EventBinding<ModeChangedEvent> modeChangedBinding;

    public static void Allow(InteractionGroup groups)
    {
        Allowed = groups;
        AllowedChanged?.Invoke();
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        modeChangedBinding = new EventBinding<ModeChangedEvent>(data => Apply(data.mode));
    }

    private void OnEnable()
    {
        UEventBus<ModeChangedEvent>.Register(modeChangedBinding);
        AllowedChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        UEventBus<ModeChangedEvent>.Deregister(modeChangedBinding);
        AllowedChanged -= Refresh;
    }

    private void Refresh() => Apply(GameModeHandler.Current);

    private void Apply(GameMode mode)
    {
        var isActive = Array.IndexOf(activeIn, mode) >= 0 && (Allowed & group) != 0;
        canvasGroup.interactable = isActive;
        canvasGroup.blocksRaycasts = isActive;
    }
}
