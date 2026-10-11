using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BurnoutMeter : MonoBehaviour
{
    private const float Max = 100f;

    [SerializeField] private Slider slider;
    [SerializeField, Min(0f)] private float increaseDuration = 2f;
    [SerializeField, Min(0f)] private float decreaseDuration = 2f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private Tween tween;
    private bool gameOver;

    public float Burnout { get; private set; }

    private void Awake()
    {
        slider.minValue = 0f;
        slider.maxValue = Max;
        slider.interactable = false;
        slider.value = Burnout;
    }

    public void IncreaseBurnout(float amount) => SetBurnout(Burnout + amount, increaseDuration);

    public void DecreaseBurnout(float amount) => SetBurnout(Burnout - amount, decreaseDuration);

    private void SetBurnout(float value, float duration)
    {
        if (gameOver) return;

        Burnout = Mathf.Clamp(value, 0f, Max);
        tween?.Kill();
        tween = slider.DOValue(Burnout, duration).SetEase(ease).OnComplete(CheckGameOver);
    }

    private void CheckGameOver()
    {
        if (Burnout < Max) return;

        gameOver = true;
        UEventBus<GameOverEvent>.Raise(new GameOverEvent());
    }

    private void OnDestroy() => tween?.Kill();
}
