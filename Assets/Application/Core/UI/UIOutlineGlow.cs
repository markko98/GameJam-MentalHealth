using JeffGrawAssets.FlexibleUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;
using UnityEngine.UI;

// Outline and glow along an icon's silhouette, taken from the icon's own sprite alpha.
// Put it on a child of the icon ("Outline"): it picks up the icon and its Button by itself, and
// when the icon is a FlexibleImage it follows that image's procedural animation.
// Inner Fill 0 keeps the icon itself untouched; raise it to light the icon up as well.
// Drive it from code with Intensity / SetHighlighted when nothing should follow the pointer.
[ExecuteAlways]
[DisallowMultipleComponent]
public class UIOutlineGlow : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler
{
    private const string ShaderName = "UI/Outline Glow";
    private static readonly Vector3[] Corners = new Vector3[4];
    private static readonly int SpriteUVId = Shader.PropertyToID("_SpriteUV");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int ExtraId = Shader.PropertyToID("_Extra");

    [Tooltip("The icon this traces. Empty = the parent's graphic, or the sprite below.")]
    [SerializeField] private Graphic target;
    [SerializeField] private Sprite sprite;

    [Header("Shape (in sprite pixels)")]
    [SerializeField, Min(0f)] private float outlineWidth = 4f;
    [SerializeField, Min(0f)] private float softness = 1.5f;
    [SerializeField, Min(0f)] private float glowWidth = 16f;
    [SerializeField, Range(0f, 1f)] private float glowStrength = 0.65f;
    [Tooltip("How much of the glow shows over the icon itself. 0 = outline only, around it.")]
    [SerializeField, Range(0f, 1f)] private float innerFill;

    [Header("Highlight")]
    [SerializeField, Range(0f, 1f)] private float idleIntensity;
    [SerializeField, Range(0f, 1f)] private float hoverIntensity = 1f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.12f;
    [Tooltip("Button whose highlight drives the glow. Found in the parents when left empty.")]
    [SerializeField] private Selectable selectable;
    [Tooltip("Off = only Intensity / SetHighlighted move it.")]
    [SerializeField] private bool followPointer = true;
    [Tooltip("Edit mode only: shows the highlighted look while you tune the shape.")]
    [SerializeField] private bool previewHighlighted = true;

    private Material instanceMaterial;
    private float intensity;
    private bool isHovered;
    private Rect lastTargetRect;
    private Sprite lastSprite;
    private Matrix4x4 lastMatrix = Matrix4x4.identity;

    public Sprite ActiveSprite => target is Image image && image.sprite != null ? image.sprite : sprite;

    public override Texture mainTexture
    {
        get
        {
            var active = ActiveSprite;
            return active != null ? active.texture : s_WhiteTexture;
        }
    }

    // One material per glow: the shape values live on it, so no extra Canvas vertex channels are needed.
    public override Material material
    {
        get
        {
            var template = base.material;
            var shader = template != null && template.shader != null && template.shader.name == ShaderName
                ? template.shader
                : GlowShader;

            if (shader == null) return template;

            if (instanceMaterial == null || instanceMaterial.shader != shader)
            {
                DestroyInstanceMaterial();
                instanceMaterial = new Material(shader)
                {
                    name = $"{name} (outline glow)",
                    hideFlags = HideFlags.DontSave
                };
            }

            return instanceMaterial;
        }
        set => base.material = value;
    }

    public float Intensity
    {
        get => intensity;
        set
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(intensity, value)) return;

            intensity = value;
            SetVerticesDirty();
        }
    }

    public void SetHighlighted(bool highlighted) => isHovered = highlighted;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (followPointer) isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (followPointer) isHovered = false;
    }

    [ContextMenu("Log Glow Setup")]
    public void LogSetup()
    {
        var active = ActiveSprite;
        var shader = material != null ? material.shader : null;
        var matrix = target is FlexibleImage flexible ? flexible.FollowerTransformationMatrix : Matrix4x4.identity;
        Debug.Log($"[UIOutlineGlow] {name}: target={(target != null ? target.name : "none")}, " +
                  $"sprite={(active != null ? active.name : "none")}, shader={(shader != null ? shader.name : "MISSING")}, " +
                  $"intensity={CurrentIntensity:0.00}, colorAlpha={color.a:0.00}, " +
                  $"rect={TargetRect()}, following={IsFollowable(matrix)}, enabled={isActiveAndEnabled}", this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        AutoWire();
        isHovered = false;
        intensity = RestingIntensity;
        if (Application.isPlaying && selectable != null) UIOutlineGlowRelay.Attach(selectable, this);
        SetAllDirty();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        DestroyInstanceMaterial();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DestroyInstanceMaterial();
    }

    protected override void UpdateMaterial()
    {
        ApplyShapeToMaterial();
        base.UpdateMaterial();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var active = ActiveSprite;
        if (active == null) return;

        var spritePixels = active.rect.size;
        var inner = TargetRect();
        if (spritePixels.x <= 0f || spritePixels.y <= 0f || inner.width <= 0f || inner.height <= 0f) return;

        // Widths are in sprite pixels, so convert them through the icon's on-screen size.
        var scale = new Vector2(inner.width / spritePixels.x, inner.height / spritePixels.y);
        var reach = outlineWidth + softness + glowWidth + 1f;
        var pad = new Vector2(reach * scale.x, reach * scale.y);

        var uv = DataUtility.GetOuterUV(active);
        var uvMin = new Vector2(uv.x, uv.y);
        var uvSize = new Vector2(uv.z - uv.x, uv.w - uv.y);

        // Sprite space: 0..1 is the icon, the padding around it reaches outside.
        var spriteMin = new Vector2(-pad.x / inner.width, -pad.y / inner.height);
        var spriteMax = new Vector2(1f - spriteMin.x, 1f - spriteMin.y);

        var tint = color;
        tint.a *= CurrentIntensity;

        AddVert(vh, new Vector2(inner.xMin - pad.x, inner.yMin - pad.y), new Vector2(spriteMin.x, spriteMin.y), uvMin, uvSize, tint);
        AddVert(vh, new Vector2(inner.xMin - pad.x, inner.yMax + pad.y), new Vector2(spriteMin.x, spriteMax.y), uvMin, uvSize, tint);
        AddVert(vh, new Vector2(inner.xMax + pad.x, inner.yMax + pad.y), new Vector2(spriteMax.x, spriteMax.y), uvMin, uvSize, tint);
        AddVert(vh, new Vector2(inner.xMax + pad.x, inner.yMin - pad.y), new Vector2(spriteMax.x, spriteMin.y), uvMin, uvSize, tint);

        FollowTargetAnimation(vh);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    private static void AddVert(VertexHelper vh, Vector2 position, Vector2 spriteSpace, Vector2 uvMin, Vector2 uvSize, Color tint)
    {
        var uv0 = uvMin + spriteSpace * uvSize;
        vh.AddVert(position, tint, uv0);
    }

    // Same transformation FlexibleImage hands its own followers, so the outline bends, slides and
    // scales with whatever its animation states do.
    private void FollowTargetAnimation(VertexHelper vh)
    {
        if (!(target is FlexibleImage flexible)) return;

        var matrix = flexible.FollowerTransformationMatrix;
        lastMatrix = matrix;
        if (!IsFollowable(matrix)) return;

        var ownScale = transform.lossyScale;
        var targetScale = flexible.transform.lossyScale;
        var local = transform.localPosition;
        var inTargetSpace = new Vector2(
            Mathf.Approximately(ownScale.x, 0f) ? 0f : local.x / ownScale.x * targetScale.x,
            Mathf.Approximately(ownScale.y, 0f) ? 0f : local.y / ownScale.y * targetScale.y);
        var centerDiff = (Vector3)(inTargetSpace - flexible.FinalMeshCenter);

        var vertex = new UIVertex();
        for (var i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.position = matrix.MultiplyPoint3x4(vertex.position + centerDiff) - centerDiff;
            vh.SetUIVertex(vertex, i);
        }
    }

    // FlexibleImage only fills this in while generating its own mesh, so until then it is all
    // zeros - applying that would collapse the quad into a point.
    private static bool IsFollowable(Matrix4x4 matrix) =>
        !Mathf.Approximately(matrix.m33, 0f) && matrix != Matrix4x4.identity;

    private void ApplyShapeToMaterial()
    {
        var glowMaterial = material;
        if (glowMaterial == null || glowMaterial != instanceMaterial) return;

        var active = ActiveSprite;
        if (active == null) return;

        var spritePixels = active.rect.size;
        if (spritePixels.x <= 0f || spritePixels.y <= 0f) return;

        var uv = DataUtility.GetOuterUV(active);
        glowMaterial.SetVector(SpriteUVId, new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
        glowMaterial.SetVector(ShapeId, new Vector4(
            outlineWidth / spritePixels.x,
            Mathf.Max(0.01f, softness) / spritePixels.x,
            glowWidth / spritePixels.x,
            glowStrength));
        glowMaterial.SetVector(ExtraId, new Vector4(spritePixels.x / spritePixels.y, innerFill, 0f, 0f));
    }

    private float CurrentIntensity
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return RestingIntensity;
#endif
            return intensity;
        }
    }

    private float RestingIntensity
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return previewHighlighted ? hoverIntensity : idleIntensity;
#endif
            return idleIntensity;
        }
    }

    // The icon it follows is a parent, so pointer events never reach this object - the relay on the
    // Button reports the highlight instead.
    private void AutoWire()
    {
        if (target == null && transform.parent != null && transform.parent.TryGetComponent<Graphic>(out var parentGraphic))
            target = parentGraphic;

        if (selectable == null) selectable = GetComponentInParent<Selectable>();
    }

    private void LateUpdate()
    {
        if (!(target is FlexibleImage flexible)) return;

        var matrix = flexible.FollowerTransformationMatrix;
        if (matrix == lastMatrix) return;

        lastMatrix = matrix;
        SetVerticesDirty();
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            var goal = isHovered ? hoverIntensity : idleIntensity;
            if (!Mathf.Approximately(intensity, goal))
            {
                Intensity = fadeDuration > 0f
                    ? Mathf.MoveTowards(intensity, goal, Time.unscaledDeltaTime / fadeDuration)
                    : goal;
            }
        }

        if (target == null) return;

        var rect = TargetRect();
        if (rect == lastTargetRect && ActiveSprite == lastSprite) return;

        lastTargetRect = rect;
        lastSprite = ActiveSprite;
        SetAllDirty();
    }

    private Rect TargetRect()
    {
        if (target == null) return rectTransform.rect;

        var targetTransform = target.rectTransform;
        targetTransform.GetLocalCorners(Corners);
        var min = (Vector2)rectTransform.InverseTransformPoint(targetTransform.TransformPoint(Corners[0]));
        var max = (Vector2)rectTransform.InverseTransformPoint(targetTransform.TransformPoint(Corners[2]));
        return new Rect(min, max - min);
    }

    private void DestroyInstanceMaterial()
    {
        if (instanceMaterial == null) return;

        if (Application.isPlaying) Destroy(instanceMaterial);
        else DestroyImmediate(instanceMaterial);
        instanceMaterial = null;
    }

    private static Shader GlowShader
    {
        get
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null) shader = Resources.Load<Shader>("UIOutlineGlow");
            if (shader == null) Debug.LogError($"[UIOutlineGlow] Shader \"{ShaderName}\" not found.");
            return shader;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        raycastTarget = false;
        AutoWire();
        SetAllDirty();
    }

    protected override void Reset()
    {
        base.Reset();
        raycastTarget = false;
        color = new Color(1f, 0.85f, 0.45f, 1f);
        AutoWire();
    }
#endif
}
