using System;
using System.Collections.Generic;
using JeffGrawAssets.FlexibleUI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Canvas parallax: one child per layer, created and sorted from the layer list. A layer is either
// a prefab - it keeps its own visuals and children, the script only places and slides it - or just
// a sprite the script shows in a FlexibleImage of its own. layers[0] is the farthest background,
// the last entry the closest foreground. A child is only recreated when its prefab reference
// changes, so scene and prefab edits survive a rebuild.
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIParallax : MonoBehaviour
{
    private const string ChildPrefix = "Layer_";

    public enum LayerKind
    {
        Decoration,
        Interactable
    }

    public enum LayerFit
    {
        FillParent,
        KeepOwnLayout
    }

    [Serializable]
    public class Layer
    {
        public string name = "Layer";
        [Tooltip("Prefab shown for this layer. Empty = the script builds a FlexibleImage from the sprite below.")]
        public GameObject prefab;
        [Tooltip("Only used when there is no prefab.")]
        public Sprite sprite;
        [Tooltip("Only used when there is no prefab.")]
        public Color tint = Color.white;
        [Tooltip("Interactable layers take clicks and hover; decoration layers ignore the pointer.")]
        public LayerKind kind = LayerKind.Decoration;
        [Tooltip("Fill Parent stretches the layer over the parent (full-screen art). Keep Own " +
                 "Layout leaves the prefab's size and position alone and only slides it.")]
        public LayerFit fit = LayerFit.FillParent;
        [Tooltip("0 = stays put (far away), 1 = moves the full Max Offset (closest).")]
        [Range(0f, 1f)] public float depth = 0.5f;
        [Tooltip("Off deactivates the layer without removing it from the list.")]
        public bool visible = true;

        [Header("Interactable only")]
        [Tooltip("Only pixels above this alpha react to the pointer. Needs Read/Write Enabled on " +
                 "the texture. 0 = the whole rect reacts.")]
        [Range(0f, 1f)] public float alphaHitThreshold;
        public UnityEvent onClick;
        public UnityEvent onPointerEnter;
        public UnityEvent onPointerExit;
    }

    [Serializable]
    private class Child
    {
        public RectTransform root;
        public GameObject prefab;
        public string layerName;
        public Vector2 basePosition;
        public Vector2 appliedOffset;
    }

    [Tooltip("Back to front. Reorder, add or remove entries and the children follow.")]
    [SerializeField] private List<Layer> layers = new List<Layer>();

    [Header("Movement")]
    [Tooltip("How far a depth-1 layer shifts from the centre, in canvas pixels.")]
    [SerializeField, Min(0f)] private float maxOffset = 60f;
    [Tooltip("Seconds the layers take to catch up with the pointer.")]
    [SerializeField, Min(0f)] private float smoothTime = 0.45f;
    [Tooltip("On = the layers drift away from the pointer.")]
    [SerializeField] private bool invert = true;
    [SerializeField] private bool horizontal = true;
    [SerializeField] private bool vertical = true;

    [Header("Idle sway")]
    [Tooltip("Drift that keeps the scene alive when the pointer is still, in pixels. 0 = off.")]
    [SerializeField, Min(0f)] private float idleSway = 6f;
    [Tooltip("Sway cycles per second.")]
    [SerializeField, Min(0f)] private float idleSwaySpeed = 0.12f;

    [Header("Layout")]
    [Tooltip("Extra pixels on top of the margin a Fill Parent layer needs to travel.")]
    [SerializeField, Min(0f)] private float extraPadding = 2f;

    [SerializeField, HideInInspector] private List<Child> children = new List<Child>();

    private Vector2 focus;
    private Vector2 focusVelocity;
    private Vector2? externalFocus;
    private float strength = 1f;
    private bool syncQueued;

    public float Strength
    {
        get => strength;
        set => strength = Mathf.Clamp01(value);
    }

    public int LayerCount => layers.Count;

    public event Action<int> LayerClicked;
    public event Action<int, bool> LayerHovered;

    public RectTransform GetLayer(int index) => index >= 0 && index < children.Count ? children[index].root : null;

    public RectTransform GetLayer(string layerName) => GetLayer(IndexOf(layerName));

    public FlexibleImage GetLayerImage(int index)
    {
        var root = GetLayer(index);
        return root != null ? root.GetComponentInChildren<FlexibleImage>(true) : null;
    }

    public FlexibleImage GetLayerImage(string layerName) => GetLayerImage(IndexOf(layerName));

    public int IndexOf(string layerName)
    {
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i] != null && layers[i].name == layerName) return i;
        }

        return -1;
    }

    public void SetFocus(Vector2 normalizedFocus) => externalFocus = new Vector2(
        Mathf.Clamp(normalizedFocus.x, -1f, 1f), Mathf.Clamp(normalizedFocus.y, -1f, 1f));

    public void ClearFocus() => externalFocus = null;

    public void SetInteractable(int index, bool interactable)
    {
        var root = GetLayer(index);
        if (root != null && root.TryGetComponent<UIParallaxLayer>(out var input)) input.interactable = interactable;
    }

    internal void NotifyClick(int index)
    {
        if (index < 0 || index >= layers.Count) return;

        layers[index].onClick?.Invoke();
        LayerClicked?.Invoke(index);
    }

    internal void NotifyHover(int index, bool entered)
    {
        if (index < 0 || index >= layers.Count) return;

        if (entered) layers[index].onPointerEnter?.Invoke();
        else layers[index].onPointerExit?.Invoke();
        LayerHovered?.Invoke(index, entered);
    }

    [ContextMenu("Rebuild Layers")]
    public void Rebuild()
    {
        MatchChildrenToLayers();

        for (var i = children.Count - 1; i >= layers.Count; i--)
        {
            DestroyChild(children[i]);
            children.RemoveAt(i);
        }

        while (children.Count < layers.Count) children.Add(new Child());

        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i] == null) layers[i] = new Layer();
            EnsureChild(i);
            ApplyLayer(i);
        }

        RemoveOrphans();
        ApplyOffsets();
    }

    // Children follow their layer by name, so reordering the list keeps whatever was set up on the
    // child - prefab overrides, FlexibleImage hover animation - with the layer it belongs to.
    private void MatchChildrenToLayers()
    {
        var ordered = new List<Child>(layers.Count);
        var taken = new bool[children.Count];

        for (var i = 0; i < layers.Count; i++)
        {
            var layerName = layers[i] != null ? layers[i].name : null;
            var match = -1;

            if (!string.IsNullOrWhiteSpace(layerName))
            {
                for (var j = 0; j < children.Count; j++)
                {
                    if (taken[j] || children[j].root == null || children[j].layerName != layerName) continue;
                    match = j;
                    break;
                }
            }

            if (match >= 0) taken[match] = true;
            ordered.Add(match >= 0 ? children[match] : null);
        }

        // Unmatched children (a renamed layer, or layers without a name) fill the empty slots in
        // order; whatever is left over lands at the tail and is trimmed away.
        var spare = new Queue<Child>();
        for (var j = 0; j < children.Count; j++)
        {
            if (!taken[j]) spare.Enqueue(children[j]);
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i] == null) ordered[i] = spare.Count > 0 ? spare.Dequeue() : new Child();
        }

        while (spare.Count > 0) ordered.Add(spare.Dequeue());

        children = ordered;
    }

    private void EnsureChild(int index)
    {
        var child = children[index];
        if (child.root != null && child.prefab == layers[index].prefab) return;

        DestroyChild(child);
        child.root = CreateChild(layers[index]);
        child.prefab = layers[index].prefab;
        child.appliedOffset = Vector2.zero;
    }

    private RectTransform CreateChild(Layer layer)
    {
        GameObject instance;
        if (layer.prefab != null)
        {
#if UNITY_EDITOR
            // InstantiatePrefab keeps the instance linked to the asset, so prefab edits still apply.
            instance = !Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(layer.prefab)
                ? (GameObject)PrefabUtility.InstantiatePrefab(layer.prefab, transform)
                : Instantiate(layer.prefab, transform);
#else
            instance = Instantiate(layer.prefab, transform);
#endif
        }
        else
        {
            instance = new GameObject(ChildPrefix, typeof(RectTransform), typeof(CanvasRenderer), typeof(FlexibleImage));
            instance.transform.SetParent(transform, false);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(instance, "Add Parallax Layer");
#endif

        return instance.transform as RectTransform ?? instance.AddComponent<RectTransform>();
    }

    private void ApplyLayer(int index)
    {
        var layer = layers[index];
        var child = children[index];
        var rect = child.root;

        child.layerName = layer.name;
        rect.gameObject.name = ChildPrefix + index + (string.IsNullOrWhiteSpace(layer.name) ? string.Empty : "_" + layer.name);
        rect.SetSiblingIndex(index);
        if (rect.gameObject.activeSelf != layer.visible) rect.gameObject.SetActive(layer.visible);

        if (layer.prefab == null) ApplySprite(layer, rect);
        if (layer.fit == LayerFit.FillParent) StretchToParent(layer, rect);

        child.basePosition = layer.fit == LayerFit.FillParent
            ? Vector2.zero
            : rect.anchoredPosition - child.appliedOffset;

        ApplyInteraction(index, layer, rect);
    }

    private void ApplySprite(Layer layer, RectTransform rect)
    {
        var image = rect.GetComponent<FlexibleImage>() ?? rect.gameObject.AddComponent<FlexibleImage>();
        image.sprite = layer.sprite;
        image.color = layer.tint;
        image.type = Image.Type.Simple;
        image.enabled = layer.sprite != null;
    }

    private void StretchToParent(Layer layer, RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        // Grown by exactly how far it can travel, so sliding never uncovers the parent's edge.
        var padding = (maxOffset + idleSway) * layer.depth + extraPadding;
        rect.sizeDelta = new Vector2(padding * 2f, padding * 2f);
    }

    private void ApplyInteraction(int index, Layer layer, RectTransform rect)
    {
        var input = rect.GetComponent<UIParallaxLayer>();

        if (layer.kind != LayerKind.Interactable)
        {
            foreach (var flexible in rect.GetComponentsInChildren<FlexibleImage>(true))
            {
                if (flexible.selectable == input) flexible.selectable = null;
            }

            if (input != null) DestroyNow(input);
            if (layer.prefab == null && rect.TryGetComponent<Graphic>(out var graphic)) graphic.raycastTarget = false;
            return;
        }

        if (input == null) input = AddLayerInput(rect.gameObject);
        input.parallax = this;
        input.index = index;
        input.transition = Selectable.Transition.None;

        var target = rect.GetComponent<Graphic>() ?? rect.GetComponentInChildren<Graphic>(true);
        input.targetGraphic = target;
        if (target != null) target.raycastTarget = true;
        if (target is Image image) image.alphaHitTestMinimumThreshold = layer.alphaHitThreshold;

        // Hover and press states of any FlexibleImage that isn't already driven by its own Selectable.
        foreach (var flexible in rect.GetComponentsInChildren<FlexibleImage>(true))
        {
            if (flexible.animationMode == FlexibleImage.AnimationStateDrivenBy.Selectable && flexible.selectable == null)
                flexible.selectable = input;
        }
    }

    private static UIParallaxLayer AddLayerInput(GameObject target)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return Undo.AddComponent<UIParallaxLayer>(target);
#endif
        return target.AddComponent<UIParallaxLayer>();
    }

    private void Awake()
    {
        Rebuild();
        focus = CurrentFocus();
    }

    private void OnEnable() => ApplyOffsets();

    private void Update()
    {
        focus = smoothTime > 0f
            ? Vector2.SmoothDamp(focus, CurrentFocus(), ref focusVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime)
            : CurrentFocus();

        ApplyOffsets();
    }

    private Vector2 CurrentFocus()
    {
        if (externalFocus.HasValue) return externalFocus.Value;

        var pointer = Pointer.current;
        if (pointer == null || Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;

        var position = pointer.position.ReadValue();
        var normalized = new Vector2(
            Mathf.Clamp(position.x / Screen.width * 2f - 1f, -1f, 1f),
            Mathf.Clamp(position.y / Screen.height * 2f - 1f, -1f, 1f));

        return invert ? -normalized : normalized;
    }

    private void ApplyOffsets()
    {
        var sway = IdleSwayOffset();

        for (var i = 0; i < children.Count && i < layers.Count; i++)
        {
            var child = children[i];
            if (child.root == null) continue;

            var offset = (focus * maxOffset + sway) * (layers[i].depth * strength);
            if (!horizontal) offset.x = 0f;
            if (!vertical) offset.y = 0f;

            child.appliedOffset = offset;
            child.root.anchoredPosition = child.basePosition + offset;
        }
    }

    private Vector2 IdleSwayOffset()
    {
        if (idleSway <= 0f || idleSwaySpeed <= 0f || !Application.isPlaying) return Vector2.zero;

        var time = Time.unscaledTime * idleSwaySpeed * Mathf.PI * 2f;
        return new Vector2(Mathf.Sin(time), Mathf.Sin(time * 0.7f)) * idleSway;
    }

    private void RemoveOrphans()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (!child.name.StartsWith(ChildPrefix, StringComparison.Ordinal)) continue;
            if (children.Exists(tracked => tracked.root != null && tracked.root == child)) continue;

            DestroyNow(child.gameObject);
        }
    }

    private static void DestroyChild(Child child)
    {
        if (child == null) return;

        if (child.root != null) DestroyNow(child.root.gameObject);
        child.root = null;
        child.prefab = null;
    }

    private static void DestroyNow(UnityEngine.Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
            return;
        }

#if UNITY_EDITOR
        Undo.DestroyObjectImmediate(target);
#else
        DestroyImmediate(target);
#endif
    }

#if UNITY_EDITOR
    // GameObjects may not be created or destroyed from inside OnValidate, hence the delay.
    private void OnValidate()
    {
        if (syncQueued) return;

        syncQueued = true;
        EditorApplication.delayCall += SyncFromInspector;
    }

    private void SyncFromInspector()
    {
        EditorApplication.delayCall -= SyncFromInspector;
        syncQueued = false;
        if (this == null || gameObject == null) return;

        Rebuild();
        if (!Application.isPlaying) EditorUtility.SetDirty(this);
    }

    private void Reset()
    {
        var rect = (RectTransform)transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
#endif
}
