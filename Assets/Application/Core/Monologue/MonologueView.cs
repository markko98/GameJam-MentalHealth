using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The box a monologue is shown in: pops in, types the lines, pops out.
[DisallowMultipleComponent]
public class MonologueView : MonoBehaviour
{
    [Header("Outlet")]
    [Tooltip("Fades the whole box. Usually on this same object.")]
    [SerializeField] private CanvasGroup group;
    [Tooltip("The box itself - this is what scales and slides.")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private UISubtitles subtitles;
    [Tooltip("Optional: speaker name. Hidden for lines without one.")]
    [SerializeField] private UILabel speakerLabel;
    [Tooltip("Optional: portrait image. Hidden for lines without one.")]
    [SerializeField] private Image portrait;
    [Tooltip("Optional: the little arrow shown while a line waits for a tap. It bobs by itself.")]
    [SerializeField] private GameObject continueIndicator;
    [Tooltip("Optional: full-screen button that advances the lines.")]
    [SerializeField] private Button tapArea;
    [Tooltip("Optional: disabled while no monologue is playing, so the box costs nothing when idle.")]
    [SerializeField] private Canvas canvas;

    [Header("Pop animation")]
    [SerializeField, Min(0f)] private float popInDuration = 0.4f;
    [SerializeField, Min(0f)] private float popOutDuration = 0.25f;
    [Tooltip("Where the box slides in from, in pixels.")]
    [SerializeField] private Vector2 slideFrom = new Vector2(0f, -60f);
    [Tooltip("Scale the box grows from. 1 = no scaling.")]
    [SerializeField, Range(0.1f, 1f)] private float scaleFrom = 0.9f;
    [SerializeField] private Ease popInEase = Ease.OutBack;
    [SerializeField] private Ease popOutEase = Ease.InOutSine;
    [Tooltip("Overshoot of the pop-in bounce. Lower is calmer; 1.1 is a soft cozy landing.")]
    [SerializeField, Range(0f, 3f)] private float popInOvershoot = 1.1f;
    [Tooltip("How far the continue arrow bobs up and down. 0 = still.")]
    [SerializeField, Min(0f)] private float indicatorBob = 10f;
    [SerializeField, Min(0.1f)] private float indicatorBobDuration = 0.7f;

    [Header("Input")]
    [Tooltip("A tap anywhere advances the line, not only on the box.")]
    [SerializeField] private bool advanceOnAnyTap = true;
    [Tooltip("Space / Enter advance the line too.")]
    [SerializeField] private bool advanceOnSubmitKey = true;

    private MonologueDefinition current;
    private Action finished;
    private Sequence pop;
    private Tween indicatorTween;
    private Vector2 panelBasePosition;
    private Vector3 panelBaseScale = Vector3.one;
    private Vector2 indicatorBasePosition;
    private bool skippable = true;
    private int lastAdvanceFrame = -1;

    public bool IsPlaying => current != null;
    public bool IsWaitingForNext => current != null && subtitles != null && subtitles.IsWaitingForNext;
    public MonologueDefinition Current => current;

    public void Play(MonologueDefinition monologue, Action onFinished = null)
    {
        if (monologue == null || !monologue.HasLines)
        {
            onFinished?.Invoke();
            return;
        }

        if (current != null) Cancel(true);

        current = monologue;
        finished = onFinished;
        skippable = monologue.skippable;

        ApplyPacing(monologue);
        ShowSpeaker(monologue.speaker, monologue.portrait);
        SetIndicatorVisible(false);
        Show();

        if (monologue.openSound.IsValid) AudioService.Play(monologue.openSound);

        pop?.Kill();
        pop = PopIn().OnComplete(() => subtitles.Play(monologue.lines, Close));
    }

    public void Next()
    {
        if (current == null || subtitles == null) return;
        if (!skippable && subtitles.IsTyping) return;

        if (lastAdvanceFrame == Time.frameCount) return;
        lastAdvanceFrame = Time.frameCount;

        subtitles.Next();
    }

    public void Finish()
    {
        if (current == null) return;

        if (subtitles.IsPlaying) subtitles.Finish();
        else Close();
    }

    public void Cancel(bool instant = false)
    {
        current = null;
        finished = null;
        if (subtitles != null) subtitles.Clear();
        SetIndicatorVisible(false);

        pop?.Kill();
        if (instant)
        {
            Hide();
            return;
        }

        pop = PopOut().OnComplete(Hide);
    }

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (panel == null) panel = transform as RectTransform;
        if (subtitles == null) subtitles = GetComponentInChildren<UISubtitles>(true);

        panelBasePosition = panel.anchoredPosition;
        panelBaseScale = panel.localScale;
        if (continueIndicator != null && continueIndicator.transform is RectTransform indicator)
            indicatorBasePosition = indicator.anchoredPosition;

        if (subtitles != null) subtitles.LineStarted += OnLineStarted;
        if (tapArea != null) tapArea.onClick.AddListener(Next);

        Hide();
    }

    private void OnDestroy()
    {
        if (subtitles != null) subtitles.LineStarted -= OnLineStarted;
        if (tapArea != null) tapArea.onClick.RemoveListener(Next);
        pop?.Kill();
        indicatorTween?.Kill();
    }

    private void Update()
    {
        if (current == null) return;

        SetIndicatorVisible(IsWaitingForNext);

        if (advanceOnAnyTap && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Next();
            return;
        }

        if (!advanceOnSubmitKey || Keyboard.current == null) return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame) Next();
    }

    private void Close()
    {
        if (current == null) return;

        var closeSound = current.closeSound;
        var callback = finished;
        current = null;
        finished = null;

        if (closeSound.IsValid) AudioService.Play(closeSound);
        SetIndicatorVisible(false);

        pop?.Kill();
        pop = PopOut().OnComplete(() =>
        {
            Hide();
            callback?.Invoke();
        });
    }

    private Sequence PopIn()
    {
        panel.anchoredPosition = panelBasePosition + slideFrom;
        panel.localScale = panelBaseScale * scaleFrom;
        group.alpha = 0f;

        var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        sequence.Join(group.DOFade(1f, Mathf.Max(0.01f, popInDuration * 0.6f)).SetEase(Ease.OutSine));
        sequence.Join(panel.DOAnchorPos(panelBasePosition, popInDuration).SetEase(popInEase, popInOvershoot));
        sequence.Join(panel.DOScale(panelBaseScale, popInDuration).SetEase(popInEase, popInOvershoot));
        return sequence;
    }

    private Sequence PopOut()
    {
        var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        sequence.Join(group.DOFade(0f, Mathf.Max(0.01f, popOutDuration)).SetEase(Ease.InSine));
        sequence.Join(panel.DOAnchorPos(panelBasePosition + slideFrom * 0.5f, popOutDuration).SetEase(popOutEase));
        sequence.Join(panel.DOScale(panelBaseScale * scaleFrom, popOutDuration).SetEase(popOutEase));
        return sequence;
    }

    private void Show()
    {
        if (canvas != null) canvas.enabled = true;
        group.blocksRaycasts = true;
        group.interactable = true;
    }

    private void Hide()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        panel.anchoredPosition = panelBasePosition;
        panel.localScale = panelBaseScale;
        if (canvas != null) canvas.enabled = false;
    }

    private void ApplyPacing(MonologueDefinition monologue)
    {
        subtitles.ResetSettings();
        subtitles.AutoAdvance = monologue.autoAdvance;
        subtitles.WaitForVoice = monologue.waitForVoice;

        if (monologue.charactersPerSecond > 0f) subtitles.CharactersPerSecond = monologue.charactersPerSecond;
        if (monologue.minLineDuration > 0f) subtitles.MinLineDuration = monologue.minLineDuration;
        if (monologue.secondsPerCharacter > 0f) subtitles.SecondsPerCharacter = monologue.secondsPerCharacter;
        if (monologue.lineFadeDuration > 0f) subtitles.FadeDuration = monologue.lineFadeDuration;
    }

    private void OnLineStarted(MonologueLine line)
    {
        var speaker = string.IsNullOrWhiteSpace(line.speaker) ? current?.speaker : line.speaker;
        var sprite = line.portrait != null ? line.portrait : current?.portrait;
        ShowSpeaker(speaker, sprite);
    }

    private void ShowSpeaker(string speaker, Sprite sprite)
    {
        if (speakerLabel != null)
        {
            var hasSpeaker = !string.IsNullOrWhiteSpace(speaker);
            speakerLabel.gameObject.SetActive(hasSpeaker);
            if (hasSpeaker) speakerLabel.Text = speaker;
        }

        if (portrait == null) return;
        portrait.sprite = sprite;
        portrait.gameObject.SetActive(sprite != null);
    }

    private void SetIndicatorVisible(bool visible)
    {
        if (continueIndicator == null || continueIndicator.activeSelf == visible) return;

        continueIndicator.SetActive(visible);
        indicatorTween?.Kill();
        indicatorTween = null;

        if (!(continueIndicator.transform is RectTransform indicator)) return;
        indicator.anchoredPosition = indicatorBasePosition;
        if (!visible || indicatorBob <= 0f) return;

        indicatorTween = indicator.DOAnchorPosY(indicatorBasePosition.y - indicatorBob, indicatorBobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(continueIndicator);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        group = GetComponent<CanvasGroup>();
        canvas = GetComponent<Canvas>();
        subtitles = GetComponentInChildren<UISubtitles>(true);
        tapArea = GetComponentInChildren<Button>(true);
    }
#endif
}
