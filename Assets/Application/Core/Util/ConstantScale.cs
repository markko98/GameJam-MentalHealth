using DG.Tweening;
using UnityEngine;

// Loops a gentle punch-scale on a target transform — e.g. to draw attention to
// a button or reward icon.
public class ConstantScale : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float scaleAmount = 0.5f;
    [SerializeField] private float duration = 1f;

    private Tweener tween;

    private void OnEnable()
    {
        if (target == null) target = transform;

        target.localScale = Vector3.one;
        tween = target.DOPunchScale(Vector3.one * scaleAmount, duration, 1, 1).SetLoops(-1);
    }

    private void OnDisable() => tween.Kill();

    private void OnDestroy() => tween.Kill();
}
