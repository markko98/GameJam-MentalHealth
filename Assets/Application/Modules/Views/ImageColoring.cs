using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageColoring : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler
{
    private const byte MinAlpha = 10;

    [SerializeField] private float brushRadius = 40f;
    [SerializeField] private float followSpeed = 15f;
    [SerializeField, Range(0f, 1f)] private float requiredPercent = 0.8f;

    public event Action Completed;
    public bool IsCompleted { get; private set; }

    private Image image;
    private Texture2D texture;
    private Sprite coloringSprite;
    private Color32[] colored;
    private Color32[] pixels;
    private bool[] painted;
    private int paintedCount;
    private int paintableCount;
    private bool hovering;
    private Vector2 brush;
    private Vector2 target;
    private int radius;

    public void Setup(Sprite sprite)
    {
        image = GetComponent<Image>();
        Clear();

        texture = CopyTexture(sprite);
        colored = texture.GetPixels32();
        pixels = new Color32[colored.Length];
        painted = new bool[colored.Length];

        for (int i = 0; i < colored.Length; i++)
        {
            var c = colored[i];
            var gray = (byte)(c.r * 0.299f + c.g * 0.587f + c.b * 0.114f);
            pixels[i] = new Color32(gray, gray, gray, c.a);
            if (c.a > MinAlpha) paintableCount++;
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        coloringSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), sprite.pivot / sprite.rect.size, sprite.pixelsPerUnit);
        image.sprite = coloringSprite;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (texture == null || IsCompleted) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, eventData.position, eventData.enterEventCamera, out var local)) return;

        var rect = DrawnRect();
        target = new Vector2((local.x - rect.x) / rect.width * texture.width, (local.y - rect.y) / rect.height * texture.height);
        radius = Mathf.Max(1, (int)(brushRadius / rect.width * texture.width));

        if (!hovering) brush = target;
        hovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    private void OnDisable()
    {
        hovering = false;
    }

    private void Update()
    {
        if (!hovering || texture == null || IsCompleted) return;

        var next = Vector2.Lerp(brush, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        var steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(brush, next) / (radius * 0.5f)));
        var changed = false;
        for (int s = 1; s <= steps; s++)
        {
            var p = Vector2.Lerp(brush, next, (float)s / steps);
            changed |= Stamp((int)p.x, (int)p.y);
        }
        brush = next;

        if (!changed) return;

        if (paintedCount >= paintableCount * requiredPercent)
        {
            Complete();
            return;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }

    private bool Stamp(int cx, int cy)
    {
        var width = texture.width;
        var changed = false;

        for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(texture.height - 1, cy + radius); y++)
        {
            for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(width - 1, cx + radius); x++)
            {
                int dx = x - cx, dy = y - cy;
                var i = y * width + x;
                if (painted[i] || dx * dx + dy * dy > radius * radius) continue;

                painted[i] = true;
                pixels[i] = colored[i];
                if (colored[i].a > MinAlpha) paintedCount++;
                changed = true;
            }
        }

        return changed;
    }

    private void Complete()
    {
        IsCompleted = true;
        texture.SetPixels32(colored);
        texture.Apply();
        Completed?.Invoke();
    }

    private Rect DrawnRect()
    {
        var rect = image.rectTransform.rect;
        if (!image.preserveAspect) return rect;

        var aspect = (float)texture.width / texture.height;
        var size = rect.width / rect.height > aspect
            ? new Vector2(rect.height * aspect, rect.height)
            : new Vector2(rect.width, rect.width / aspect);
        var pivot = image.rectTransform.pivot;
        return new Rect(rect.x + (rect.width - size.x) * pivot.x, rect.y + (rect.height - size.y) * pivot.y, size.x, size.y);
    }

    // Copies through a RenderTexture so the source sprite doesn't need Read/Write enabled.
    private static Texture2D CopyTexture(Sprite sprite)
    {
        var source = sprite.texture;
        var rect = sprite.textureRect;
        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);

        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(rect, 0, 0);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    private void Clear()
    {
        if (coloringSprite != null) Destroy(coloringSprite);
        if (texture != null) Destroy(texture);
        paintedCount = 0;
        paintableCount = 0;
        hovering = false;
        IsCompleted = false;
    }

    private void OnDestroy()
    {
        Clear();
    }
}
