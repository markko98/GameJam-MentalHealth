using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public sealed class SceneFadeTransition : MonoBehaviour
{
    [Header("References")] [SerializeField]
    Canvas canvas;

    [SerializeField] Image overlay;

    [Header("Defaults")] [SerializeField] private FadeConfig config;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private Tween tween;

    public void Initialize(FadeConfig overrideConfig = null, Ease? easeOverride = null)
    {
        if (overrideConfig != null) config = overrideConfig;
        if (easeOverride.HasValue) ease = easeOverride.Value;
        Setup();
        FadeInstant(0f);
    }

    public void RunFadeTransition(string sceneName,
        LoadSceneMode mode,
        float duration = -1f,
        Color? color = null)
    {
        StartCoroutine(DoTransition(sceneName, mode, duration, color));
    }

    public void FadeIn(float duration = -1f, Color? color = null)
    {
        duration = duration < 0 ? config.FadeDuration : duration;
        var c = color ?? config.FadeColor;
        KillTween();
        SetColorKeepAlpha(c);
        tween = overlay.DOFade(0f, duration).SetEase(ease);
    }

    public void FadeOut(float duration = -1f, Color? color = null)
    {
        duration = duration < 0 ? config.FadeDuration : duration;
        var c = color ?? config.FadeColor;
        KillTween();
        SetColorKeepAlpha(c);
        tween = overlay.DOFade(1f, duration).SetEase(ease);
    }

    public void FadeInstant(float alpha, Color? color = null)
    {
        KillTween();
        var c = color ?? config.FadeColor;
        overlay.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
    }

    private void Setup()
    {
        if (overlay == null) return;

        overlay.raycastTarget = false;
        overlay.color = new Color(config.FadeColor.r, config.FadeColor.g, config.FadeColor.b, 0f);
    }

    private IEnumerator DoTransition(string sceneName,
        LoadSceneMode mode,
        float duration,
        Color? color)
    {
        duration = duration < 0 ? config.FadeDuration : duration;
        FadeOut(duration, color);
        if (tween != null) yield return tween.WaitForCompletion();
        var async = SceneManager.LoadSceneAsync(sceneName, mode);
        async.allowSceneActivation = false;
        overlay.raycastTarget = true;
        while (async.progress < 0.9f) yield return null;
        async.allowSceneActivation = true;
        while (!async.isDone) yield return null;
        yield return null;
        overlay.raycastTarget = false;
        FadeIn(duration, color);
        if (tween != null) yield return tween.WaitForCompletion();
    }

    private void KillTween()
    {
        if (tween != null && tween.IsActive()) tween.Kill(false);
        tween = null;
    }

    private void SetColorKeepAlpha(Color c)
    {
        if (overlay == null) return;

        overlay.color = new Color(c.r, c.g, c.b, overlay.color.a);
    }
}

[System.Serializable]
public class FadeConfig
{
    public float FadeDuration = 0.25f;
    public Color FadeColor = Color.black;
}
