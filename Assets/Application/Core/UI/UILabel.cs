using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public class UILabel : MonoBehaviour
{
    private const int AllCharacters = 99999;

    [Header("Typewriter")]
    [SerializeField, Min(1f)] private float charactersPerSecond = 40f;
    [Tooltip("Extra pause after . ! ? and half of it after , ; :")]
    [SerializeField, Min(0f)] private float punctuationPause = 0.2f;
    [Tooltip("Played as characters appear; use the sound's cooldown to thin it out.")]
    [SerializeField] private SoundId typeSound;

    private TMP_Text tmp;
    private Coroutine typing;
    private Action typingCompleted;
    private Tween fade;

    public TMP_Text Tmp => tmp != null ? tmp : tmp = GetComponent<TMP_Text>();
    public bool IsTyping => typing != null;

    public string Text
    {
        get => Tmp.text;
        set
        {
            CancelTyping();
            Tmp.text = value;
        }
    }

    public Color Color
    {
        get => Tmp.color;
        set => Tmp.color = value;
    }

    public float Alpha
    {
        get => Tmp.alpha;
        set
        {
            fade?.Kill();
            Tmp.alpha = value;
        }
    }

    public void SetNumber(int value)
    {
        CancelTyping();
        Tmp.SetText("{0}", value);
    }

    public void Clear() => Text = string.Empty;

    public void SetVisible(bool visible) => Tmp.enabled = visible;

    public void Type(string text, Action onCompleted = null)
    {
        CancelTyping();
        Tmp.text = text;
        typingCompleted = onCompleted;

        if (!isActiveAndEnabled)
        {
            CompleteTyping();
            return;
        }

        Tmp.maxVisibleCharacters = 0;
        typing = StartCoroutine(TypeRoutine());
    }

    // Reveals the rest instantly and fires the Type callback ("skip").
    public void CompleteTyping()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        Tmp.maxVisibleCharacters = AllCharacters;

        var completed = typingCompleted;
        typingCompleted = null;
        completed?.Invoke();
    }

    // Fades in from transparent, or from the current alpha if a fade is already running.
    public Tween FadeIn(float duration)
    {
        if (fade == null || !fade.IsActive()) Tmp.alpha = 0f;
        return FadeTo(1f, duration);
    }

    public Tween FadeOut(float duration) => FadeTo(0f, duration);

    public Tween FadeTo(float alpha, float duration)
    {
        fade?.Kill();
        fade = DOTween.To(() => Tmp.alpha, value => Tmp.alpha = value, alpha, duration)
            .SetUpdate(true)
            .SetLink(gameObject);
        return fade;
    }

    private void OnDisable()
    {
        CancelTyping();
        fade?.Kill();
    }

    private void CancelTyping()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        typingCompleted = null;
        Tmp.maxVisibleCharacters = AllCharacters;
    }

    private IEnumerator TypeRoutine()
    {
        Tmp.ForceMeshUpdate();
        var characters = Tmp.textInfo.characterInfo;
        var total = Tmp.textInfo.characterCount;
        var visible = 0;
        var wait = 0f;

        while (visible < total)
        {
            wait -= Time.unscaledDeltaTime;
            var revealedAny = false;

            while (wait <= 0f && visible < total)
            {
                var character = characters[visible++].character;
                wait += 1f / charactersPerSecond + PauseAfter(character);
                revealedAny |= !char.IsWhiteSpace(character);
            }

            Tmp.maxVisibleCharacters = visible;
            if (revealedAny && typeSound.IsValid) AudioService.Play(typeSound);
            yield return null;
        }

        typing = null;
        CompleteTyping();
    }

    private float PauseAfter(char character) => character switch
    {
        '.' or '!' or '?' => punctuationPause,
        ',' or ';' or ':' => punctuationPause * 0.5f,
        _ => 0f
    };
}
