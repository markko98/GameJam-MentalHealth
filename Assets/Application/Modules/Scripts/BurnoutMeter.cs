using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BurnoutMeter : MonoBehaviour
{
    public const float Max = 100f;

    [Tooltip("Filled image; its fill amount follows burnout (0..1).")]
    [SerializeField] private Image fill;
    [SerializeField, Min(0f)] private float increaseDuration = 2f;
    [SerializeField, Min(0f)] private float decreaseDuration = 2f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private Tween tween;
    private bool gameOver;

    public float Burnout { get; private set; }

    // New burnout value, raised right away (not after the bar animation).
    public event Action<float> Increased;

    private void Awake()
    {
        fill.fillAmount = Burnout / Max;
    }

    public void IncreaseBurnout(float amount) => SetBurnout(Burnout + amount, increaseDuration);

    public void DecreaseBurnout(float amount) => SetBurnout(Burnout - amount, decreaseDuration);

    private void SetBurnout(float value, float duration)
    {
        if (gameOver) return;

        var before = Burnout;
        Burnout = Mathf.Clamp(value, 0f, Max);
        if (Burnout > before) Increased?.Invoke(Burnout);
        tween?.Kill();
        tween = fill.DOFillAmount(Burnout / Max, duration).SetEase(ease).OnComplete(CheckGameOver);
    }

    private void CheckGameOver()
    {
        if (Burnout < Max) return;

        gameOver = true;
        UEventBus<GameOverEvent>.Raise(new GameOverEvent());
    }

    private void OnDestroy() => tween?.Kill();
}
