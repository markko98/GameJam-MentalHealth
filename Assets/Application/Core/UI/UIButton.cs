using System;
using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIButton : Button
{
    private const float ScaleDuration = 0.08f;
    private const float HoldDrainSpeed = 3f;

    [SerializeField] private UILabel label;
    [SerializeField] private SoundId clickSound;
    [Tooltip("Blocks repeated clicks (double taps) for this many seconds.")]
    [SerializeField, Min(0f)] private float clickCooldown = 0.2f;
    [Tooltip("Scale while held down. 1 = no press animation.")]
    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.95f;
    [Tooltip("Label alpha while the button is not interactable.")]
    [SerializeField, Range(0f, 1f)] private float disabledLabelAlpha = 0.5f;

    [Tooltip("Seconds the button must be held down to click. 0 = normal click.")]
    [SerializeField, Min(0f)] private float holdDuration;
    [Tooltip("RectMask2D covering the button; its fill child is revealed left to right while holding.")]
    [SerializeField] private RectTransform holdFill;

    private readonly UIListeners<UIButton> listeners = new UIListeners<UIButton>();
    private float nextClickTime;
    private Vector3 baseScale = Vector3.one;
    private int lockCount;
    private bool isHolding;
    private Coroutine holdRoutine;
    private RectMask2D holdMask;

    public UILabel Label => label;
    public bool IsLocked => lockCount > 0;
    public bool IsHoldButton => holdDuration > 0f;
    public float HoldProgress { get; private set; }

    public string Text
    {
        get => label != null ? label.Text : string.Empty;
        set
        {
            if (label != null) label.Text = value;
        }
    }

    public void Register(Action listener, Object owner = null) => listeners.Add(listener, _ => listener(), owner);

    // The button stays locked until the task finishes, so the action can never run twice at once.
    public void RegisterAsync(Func<Task> listener, Object owner = null) => listeners.Add(listener, _ => RunLocked(listener), owner);

    public void Unregister(Action listener) => listeners.Remove(listener);
    public void Unregister(Func<Task> listener) => listeners.Remove(listener);
    public void UnregisterAll() => listeners.Clear();

    // Locks stack: the button is usable again only after every Lock() got its Unlock().
    public void Lock()
    {
        lockCount++;
        RefreshState();
    }

    public void Unlock()
    {
        if (lockCount == 0) return;
        lockCount--;
        RefreshState();
    }

    public override bool IsInteractable() => base.IsInteractable() && lockCount == 0;

    public void Click()
    {
        if (!TryBeginClick()) return;
        onClick.Invoke();
        listeners.Invoke(this);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !IsHoldButton) Click();
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        if (!TryBeginClick()) return;
        base.OnSubmit(eventData);
        listeners.Invoke(this);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (eventData.button != PointerEventData.InputButton.Left || !IsInteractable()) return;

        ScaleTo(pressedScale);
        if (IsHoldButton) StartHold();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        ScaleTo(1f);
        isHolding = false;
        ReleaseSelection(gameObject);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        isHolding = false;
    }

    protected override void Awake()
    {
        base.Awake();
        baseScale = transform.localScale;
        if (label == null) label = GetComponentInChildren<UILabel>(true);
        if (holdFill != null) holdFill.TryGetComponent(out holdMask);
        ApplyHoldFill();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (!Application.isPlaying) return;

        transform.DOKill();
        transform.localScale = baseScale;
        isHolding = false;
        holdRoutine = null;
        HoldProgress = 0f;
        ApplyHoldFill();
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        FadeLabel(label, state == SelectionState.Disabled ? disabledLabelAlpha : 1f, instant ? 0f : colors.fadeDuration);
    }

    // Pointer presses select the button, leaving it stuck in Selected; only keyboard/gamepad should keep selection.
    internal static void ReleaseSelection(GameObject target)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == target)
            eventSystem.SetSelectedGameObject(null);
    }

    internal static void FadeLabel(UILabel target, float alpha, float duration)
    {
        if (target == null) return;
        target.Tmp.CrossFadeAlpha(alpha, Application.isPlaying ? duration : 0f, true);
    }

    private bool TryBeginClick()
    {
        if (!IsActive() || !IsInteractable() || Time.unscaledTime < nextClickTime) return false;

        nextClickTime = Time.unscaledTime + clickCooldown;
        if (clickSound.IsValid) AudioService.Play(clickSound);
        return true;
    }

    private void RefreshState()
    {
        if (!IsInteractable()) isHolding = false;
        DoStateTransition(currentSelectionState, false);
    }

    private async void RunLocked(Func<Task> listener)
    {
        Lock();
        try
        {
            await listener();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            if (this != null) Unlock();
        }
    }

    private void StartHold()
    {
        isHolding = true;
        holdRoutine ??= StartCoroutine(HoldRoutine());
    }

    private IEnumerator HoldRoutine()
    {
        while (isHolding || HoldProgress > 0f)
        {
            if (isHolding && !IsInteractable()) isHolding = false;

            var deltaTime = Time.unscaledDeltaTime;
            HoldProgress = Mathf.Clamp01(isHolding
                ? HoldProgress + deltaTime / holdDuration
                : HoldProgress - deltaTime * HoldDrainSpeed);

            if (isHolding && HoldProgress >= 1f)
            {
                isHolding = false;
                ScaleTo(1f);
                Click();
            }

            ApplyHoldFill();
            yield return null;
        }

        holdRoutine = null;
    }

    private void ApplyHoldFill()
    {
        if (holdFill == null || !Application.isPlaying) return;

        holdFill.gameObject.SetActive(HoldProgress > 0f);

        // Masking keeps the fill's rounded shape intact; stretching it would squash the corners near 0.
        if (holdMask != null) holdMask.padding = new Vector4(0f, 0f, holdFill.rect.width * (1f - HoldProgress), 0f);
        else holdFill.anchorMax = new Vector2(HoldProgress, holdFill.anchorMax.y);
    }

    private void ScaleTo(float factor)
    {
        if (pressedScale >= 1f) return;
        transform.DOKill();
        transform.DOScale(baseScale * factor, ScaleDuration).SetUpdate(true);
    }

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();
        label = GetComponentInChildren<UILabel>(true);
    }
#endif
}
