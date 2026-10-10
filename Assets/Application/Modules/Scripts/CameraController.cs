using DG.Tweening;
using UnityEngine;

// The UI canvas is Screen Space - Overlay, so the camera does not see it: "zoom" scales and slides
// the root that holds the parallax layers instead.
public class CameraController : MonoBehaviour
{
    // parallax that lazy follows mouse - done by UIParallax on the canvas' parallax parent;
    //   drive it from here with parallax.SetFocus(...) / parallax.Strength when needed
    // shake option for showing burnout - connected with meter

    [Tooltip("Scaled and moved to zoom - the Parallax object on the canvas.")]
    [SerializeField] private RectTransform zoomRoot;
    [SerializeField] private Ease zoomEase = Ease.InOutSine;

    private Sequence zoomTween;

    /// <summary>Zooms so that <paramref name="focus"/> sits in the middle of the screen (as far as the
    /// edges allow). A null focus zooms on the centre. Zoom 1 = the normal, unzoomed view.</summary>
    public void ZoomTo(RectTransform focus, float zoom, float duration)
    {
        if (zoomRoot == null) return;

        zoom = Mathf.Max(0.01f, zoom);
        var position = ClampToEdges(-FocusPoint(focus) * zoom, zoom);

        zoomTween?.Kill();

        if (duration <= 0f)
        {
            zoomRoot.localScale = new Vector3(zoom, zoom, 1f);
            zoomRoot.anchoredPosition = position;
            return;
        }

        zoomTween = DOTween.Sequence()
            .Join(zoomRoot.DOScale(new Vector3(zoom, zoom, 1f), duration))
            .Join(zoomRoot.DOAnchorPos(position, duration))
            .SetEase(zoomEase)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    public void ResetZoom(float duration) => ZoomTo(null, 1f, duration);

    // Focus centre in the zoom root's own space, measured from its pivot - unaffected by the
    // root's current scale and position.
    private Vector2 FocusPoint(RectTransform focus)
    {
        if (focus == null) return Vector2.zero;

        return zoomRoot.InverseTransformPoint(focus.TransformPoint(focus.rect.center));
    }

    // Keeps the scaled root covering the screen, so zooming near an edge never shows what is behind.
    private Vector2 ClampToEdges(Vector2 position, float zoom)
    {
        var size = zoomRoot.rect.size;
        var maxX = Mathf.Max(0f, (zoom - 1f) * size.x * 0.5f);
        var maxY = Mathf.Max(0f, (zoom - 1f) * size.y * 0.5f);

        return new Vector2(Mathf.Clamp(position.x, -maxX, maxX), Mathf.Clamp(position.y, -maxY, maxY));
    }

    private void OnDestroy() => zoomTween?.Kill();
}
