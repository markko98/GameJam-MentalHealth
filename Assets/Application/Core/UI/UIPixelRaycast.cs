using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Makes a graphic take the pointer only where its sprite is actually drawn, so the transparent
// part of a layer never blocks what is behind it. The mask is baked in the Editor, so the texture
// does not need Read/Write Enabled and costs nothing at runtime.
// Put it next to the Image / FlexibleImage, on the icon itself.
[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public class UIPixelRaycast : MonoBehaviour, ICanvasRaycastFilter
{
    [Tooltip("Pixels below this alpha let the pointer through.")]
    [SerializeField, Range(0.01f, 1f)] private float threshold = 0.1f;
    [Tooltip("Mask resolution along the sprite's longer side. Higher = sharper edges.")]
    [SerializeField, Range(16, 256)] private int resolution = 96;

    [SerializeField, HideInInspector] private int maskWidth;
    [SerializeField, HideInInspector] private int maskHeight;
    [SerializeField, HideInInspector] private byte[] mask;
    [SerializeField, HideInInspector] private Sprite bakedSprite;
    [SerializeField, HideInInspector] private float bakedThreshold;
    [SerializeField, HideInInspector] private int bakedResolution;

    private bool warned;

    public Sprite Sprite => GetComponent<Graphic>() is Image image ? image.sprite : null;
    public bool IsBaked => mask != null && mask.Length > 0 && maskWidth > 0 && maskHeight > 0;

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!IsBaked)
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning($"[UIPixelRaycast] {name} has no baked mask - right-click the component and Bake Hit Mask.", this);
            }

            return true;
        }

        var rect = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out var local)) return false;

        var area = rect.rect;
        if (area.width <= 0f || area.height <= 0f) return false;

        var u = (local.x - area.xMin) / area.width;
        var v = (local.y - area.yMin) / area.height;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

        var x = Mathf.Clamp((int)(u * maskWidth), 0, maskWidth - 1);
        var y = Mathf.Clamp((int)(v * maskHeight), 0, maskHeight - 1);
        var index = y * maskWidth + x;
        return (mask[index >> 3] & (1 << (index & 7))) != 0;
    }

#if UNITY_EDITOR
    [ContextMenu("Bake Hit Mask")]
    public void Bake()
    {
        var sprite = Sprite;
        if (sprite == null)
        {
            Debug.LogWarning($"[UIPixelRaycast] {name} has no sprite to bake from.", this);
            return;
        }

        var spriteRect = sprite.rect;
        var longer = Mathf.Max(spriteRect.width, spriteRect.height);
        var width = Mathf.Max(1, Mathf.RoundToInt(spriteRect.width / longer * resolution));
        var height = Mathf.Max(1, Mathf.RoundToInt(spriteRect.height / longer * resolution));

        var texture = sprite.texture;
        var uvScale = new Vector2(spriteRect.width / texture.width, spriteRect.height / texture.height);
        var uvOffset = new Vector2(spriteRect.x / texture.width, spriteRect.y / texture.height);

        // Through a RenderTexture, so the source texture needs no Read/Write Enabled.
        var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        Graphics.Blit(texture, renderTexture, uvScale, uvOffset);
        RenderTexture.active = renderTexture;

        var readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        var pixels = readable.GetPixels32();
        var bits = new byte[(width * height + 7) / 8];
        var cutoff = threshold * 255f;
        var solid = 0;

        for (var i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < cutoff) continue;
            bits[i >> 3] |= (byte)(1 << (i & 7));
            solid++;
        }

        DestroyImmediate(readable);

        Undo.RecordObject(this, "Bake Hit Mask");
        mask = bits;
        maskWidth = width;
        maskHeight = height;
        bakedSprite = sprite;
        bakedThreshold = threshold;
        bakedResolution = resolution;
        warned = false;
        EditorUtility.SetDirty(this);

        Debug.Log($"[UIPixelRaycast] {name}: baked {width}x{height} mask from \"{sprite.name}\", " +
                  $"{solid * 100 / Mathf.Max(1, width * height)}% of the rect takes clicks.", this);
    }

    private void OnValidate()
    {
        if (bakedSprite == Sprite && Mathf.Approximately(bakedThreshold, threshold) && bakedResolution == resolution && IsBaked) return;

        EditorApplication.delayCall += BakeFromInspector;
    }

    private void BakeFromInspector()
    {
        EditorApplication.delayCall -= BakeFromInspector;
        if (this == null || gameObject == null || Application.isPlaying) return;

        Bake();
    }

    private void Reset() => Bake();
#endif
}
