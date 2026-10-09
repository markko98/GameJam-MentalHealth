using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class SlideInRightTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (oldVc == null || newVc == null) return seq;

        float screenWidth = Screen.width;

        oldVc.view.SetActive(true);
        newVc.view.SetActive(true);
        
        newVc.view.transform.localScale = Vector3.one;
        newVc.view.transform.localPosition = new Vector3(screenWidth, 0f, 0f);

        seq.AppendInterval(delay);

        seq.Append(oldVc.view.transform
            .DOLocalMoveX(-screenWidth, duration)
            .SetEase(Ease.InOutSine));

        seq.Insert(delay, newVc.view.transform
            .DOLocalMoveX(0f, duration)
            .SetEase(Ease.InOutSine));

        return seq;
    }
}

public sealed class SlideInLeftTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (oldVc == null || newVc == null) return seq;

        float screenWidth = Screen.width;

        oldVc.view.SetActive(true);
        newVc.view.SetActive(true);

        newVc.view.transform.localScale = Vector3.one;
        newVc.view.transform.localPosition = new Vector3(-screenWidth, 0f, 0f);

        seq.AppendInterval(delay);

        seq.Append(oldVc.view.transform
            .DOLocalMoveX(screenWidth, duration)
            .SetEase(Ease.InOutSine));

        seq.Insert(delay, newVc.view.transform
            .DOLocalMoveX(0f, duration)
            .SetEase(Ease.InOutSine));

        return seq;
    }
}

public sealed class SlideInUpTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (newVc == null) return seq;

        newVc.view.SetActive(true);
        var rect = newVc.view.GetComponent<RectTransform>();
        newVc.view.transform.localScale = Vector3.one;
        rect.anchoredPosition = new Vector2(0, -Screen.height);

        seq.Append(rect.DOAnchorPosY(0, duration).SetDelay(delay).SetEase(Ease.OutCubic));
        return seq;
    }
}

public sealed class ScaleUpFromMiddleTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (newVc == null) return seq;

        newVc.view.SetActive(true);
        var rect = newVc.view.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;

        seq.AppendInterval(delay);
        seq.Append(rect.DOScale(Vector3.one, duration).SetEase(Ease.OutBack));

        // optional: punch buttons
        foreach (var b in newVc.view.GetComponentsInChildren<Button>())
            seq.Join(b.transform.DOPunchScale(Vector3.one * 0.2f, duration));

        return seq;
    }
}

public sealed class ScaleDownFromMiddleTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (oldVc == null || newVc == null) return seq;

        oldVc.view.SetActive(true);
        newVc.view.SetActive(true);

        var oldRect = oldVc.view.GetComponent<RectTransform>();
        var newRect = newVc.view.GetComponent<RectTransform>();
        newRect.localScale = Vector3.zero;

        seq.AppendInterval(delay);
        seq.Append(oldRect.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
        seq.Insert(delay + duration * 0.3f,
            newRect.DOScale(Vector3.one, duration * 0.7f).SetEase(Ease.OutBack));

        return seq;
    }
}

public sealed class NoTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (newVc == null) return seq;

        newVc.view.transform.localScale = Vector3.one;
        newVc.view.SetActive(true);

        if (delay > 0f)
            seq.AppendInterval(delay);

        return seq;
    }
}

public sealed class SlideOutDownTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (oldVc == null) return seq;

        float screenHeight = Screen.height;

        oldVc.view.SetActive(true);
        if (newVc != null) newVc.view.SetActive(true);

        seq.AppendInterval(delay);
        seq.Append(oldVc.view.transform
                .DOLocalMoveY(-screenHeight, duration)
                .SetEase(Ease.InOutCubic))
            .OnComplete(() => oldVc.view.SetActive(false));

        return seq;
    }
}

public sealed class SlideOutLeftTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (oldVc == null) return seq;

        float screenWidth = Screen.width;

        oldVc.view.SetActive(true);
        if (newVc != null) newVc.view.SetActive(true);

        seq.AppendInterval(delay);
        seq.Append(oldVc.view.transform
                .DOLocalMoveX(-screenWidth, duration)
                .SetEase(Ease.InOutSine))
            .OnComplete(() => oldVc.view.SetActive(false));

        return seq;
    }
}

public sealed class SlideOutRightTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (oldVc == null) return seq;

        float screenWidth = Screen.width;

        oldVc.view.SetActive(true);
        if (newVc != null) newVc.view.SetActive(true);

        seq.AppendInterval(delay);
        seq.Append(oldVc.view.transform
                .DOLocalMoveX(screenWidth, duration)
                .SetEase(Ease.InOutSine))
            .OnComplete(() => oldVc.view.SetActive(false));

        return seq;
    }
}
public sealed class SlideInDownTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (newVc == null) return seq;

        float screenHeight = Screen.height;

        newVc.view.SetActive(true);
        var rect = newVc.view.GetComponent<RectTransform>();
        newVc.view.transform.localScale = Vector3.one;
        rect.anchoredPosition = new Vector2(0, screenHeight);

        seq.AppendInterval(delay);
        seq.Append(rect.DOAnchorPosY(0, duration).SetEase(Ease.OutCubic));

        return seq;
    }
}

public sealed class SlideOutUpTransition : IUIControllerTransition
{
    public Sequence Build(UIViewController oldVc, UIViewController newVc, float duration, float delay)
    {
        var seq = DOTween.Sequence();
        if (oldVc == null) return seq;

        float screenHeight = Screen.height;

        oldVc.view.SetActive(true);
        if (newVc != null) newVc.view.SetActive(true);

        seq.AppendInterval(delay);
        seq.Append(oldVc.view.transform
                .DOLocalMoveY(screenHeight, duration)
                .SetEase(Ease.InOutCubic))
            .OnComplete(() => oldVc.view.SetActive(false));

        return seq;
    }
}