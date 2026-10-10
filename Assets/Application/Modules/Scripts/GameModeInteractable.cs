using UnityEngine;

// Put this on anything that should only take clicks in some modes - the desktop UI in Task, the
// cat and the photos in Break. Outside its modes the object ignores the pointer, so clicks fall
// through to whatever is behind it.
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class GameModeInteractable : MonoBehaviour
{
    [Tooltip("Modes in which this object can be clicked.")]
    [SerializeField] private GameMode[] activeIn = { GameMode.Break };

    private CanvasGroup canvasGroup;
    private EventBinding<ModeChangedEvent> modeChangedBinding;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        modeChangedBinding = new EventBinding<ModeChangedEvent>(data => Apply(data.mode));
    }

    private void OnEnable()
    {
        UEventBus<ModeChangedEvent>.Register(modeChangedBinding);
        Apply(GameModeHandler.Current);
    }

    private void OnDisable() => UEventBus<ModeChangedEvent>.Deregister(modeChangedBinding);

    private void Apply(GameMode mode)
    {
        var isActive = System.Array.IndexOf(activeIn, mode) >= 0;
        canvasGroup.interactable = isActive;
        canvasGroup.blocksRaycasts = isActive;
    }
}
