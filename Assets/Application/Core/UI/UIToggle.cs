using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIToggle : Toggle
{
    [SerializeField] private UILabel label;
    [SerializeField] private SoundId clickSound;
    [Tooltip("Label alpha while the toggle is not interactable.")]
    [SerializeField, Range(0f, 1f)] private float disabledLabelAlpha = 0.5f;

    private readonly UIListeners<bool> listeners = new UIListeners<bool>();

    public UILabel Label => label;

    public string Text
    {
        get => label != null ? label.Text : string.Empty;
        set
        {
            if (label != null) label.Text = value;
        }
    }

    public void Register(Action<bool> listener, Object owner = null) => listeners.Add(listener, listener, owner);
    public void Unregister(Action<bool> listener) => listeners.Remove(listener);
    public void UnregisterAll() => listeners.Clear();

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) PlayClickSound();
        base.OnPointerClick(eventData);
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        PlayClickSound();
        base.OnSubmit(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        UIButton.ReleaseSelection(gameObject);
    }

    protected override void Awake()
    {
        base.Awake();
        if (label == null) label = GetComponentInChildren<UILabel>(true);
        if (Application.isPlaying) onValueChanged.AddListener(listeners.Invoke);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        UIButton.FadeLabel(label, state == SelectionState.Disabled ? disabledLabelAlpha : 1f, instant ? 0f : colors.fadeDuration);
    }

    private void PlayClickSound()
    {
        if (clickSound.IsValid && IsActive() && IsInteractable()) AudioService.Play(clickSound);
    }

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();
        label = GetComponentInChildren<UILabel>(true);
    }
#endif
}
