using DG.Tweening;

public interface IUIControllerTransition
{
    /// Build and return a DOTween Sequence
    Sequence Build(UIViewController oldVc,
        UIViewController newVc,
        float duration,
        float delay);
}