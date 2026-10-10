using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Canvas parallax: one prefab instance per layer, created and sorted from the layer list.
// layers[0] is the farthest background, the last entry the closest foreground. A child is only
// recreated when its prefab reference changes, so scene and prefab edits survive a rebuild.
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
        public GameObject prefab;
        [Tooltip("Decoration turns the pointer off for everything in the layer, so it never blocks " +
                 "the layers behind it. Interactable leaves the prefab's own raycast setup alone.")]
        public LayerKind kind = LayerKind.Decoration;
        [Tooltip("Fill Parent stretches the layer over the parent (full-screen art). Keep Own " +
                 "Layout leaves the prefab's size and position alone and only slides it.")]
        public LayerFit fit = LayerFit.FillParent;
        [Tooltip("0 = stays put (far away), 1 = moves the full Max Offset (closest).")]
        [Range(0f, 1f)] public float depth = 0.5f;
        [Tooltip("Off deactivates the layer without removing it from the list.")]
        public bool visible = true;
        [Tooltip("Interactable only: pixels below this alpha let the pointer through, so the " +
                 "transparent part does not block. Needs Read/Write Enabled on the texture.")]
        [Range(0f, 1f)] public float alphaHitThreshold;
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

    private static readonly HashSet<Image> reportedTextures = new HashSet<Image>();

    private Vector2 focus;
    private Vector2 focusVelocity;
    private Vector2? externalFocus;
    private float strength = 1f;
    private bool syncQueued;

    /// <summary>Master multiplier, 0 - 1: fades the whole effect out without touching the layers.</summary>
    public float Strength
    {
        get => strength;
        set => strength = Mathf.Clamp01(value);
    }

    public int LayerCount => layers.Count;

    public RectTransform GetLayer(int index) => index >= 0 && index < children.Count ? children[index].root : null;

    public RectTransform GetLayer(string layerName) => GetLayer(IndexOf(layerName));

    public int IndexOf(string layerName)
    {
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i] != null && layers[i].name == layerName) return i;
        }

        return -1;
    }

    /// <summary>Points the parallax at a spot instead of the pointer: (-1,-1) bottom left, (1,1) top right.</summary>
    public void SetFocus(Vector2 normalizedFocus) => externalFocus = new Vector2(
        Mathf.Clamp(normalizedFocus.x, -1f, 1f), Mathf.Clamp(normalizedFocus.y, -1f, 1f));

    public void ClearFocus() => externalFocus = null;

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
            if (children[i].root != null) ApplyLayer(i);
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
        var prefab = layers[index].prefab;

        if (child.root != null && child.prefab == prefab) return;

        DestroyChild(child);
        if (prefab == null) return;

        child.root = CreateChild(prefab);
        child.prefab = prefab;
        child.appliedOffset = Vector2.zero;
    }

    private RectTransform CreateChild(GameObject prefab)
    {
#if UNITY_EDITOR
        // InstantiatePrefab keeps the instance linked to the asset, so prefab edits still apply.
        var instance = !Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(prefab)
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform)
            : Instantiate(prefab, transform);

        if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(instance, "Add Parallax Layer");
#else
        var instance = Instantiate(prefab, transform);
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

        if (layer.fit == LayerFit.FillParent) StretchToParent(layer, rect);

        child.basePosition = layer.fit == LayerFit.FillParent
            ? Vector2.zero
            : rect.anchoredPosition - child.appliedOffset;

        ApplyInteraction(layer, rect);
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

    private static void ApplyInteraction(Layer layer, RectTransform rect)
    {
        var isInteractable = layer.kind == LayerKind.Interactable;

        foreach (var graphic in rect.GetComponentsInChildren<Graphic>(true))
        {
            // A decoration layer never takes the pointer, so full-screen art cannot swallow the
            // clicks meant for the layers behind it.
            if (!isInteractable)
            {
                graphic.raycastTarget = false;
                continue;
            }

            if (layer.alphaHitThreshold <= 0f || !(graphic is Image image)) continue;

            // Unity throws instead of ignoring it when the texture cannot be read on the CPU.
            try
            {
                image.alphaHitTestMinimumThreshold = layer.alphaHitThreshold;
            }
            catch (InvalidOperationException)
            {
                if (reportedTextures.Add(image)) 
                    Debug.LogWarning($"[UIParallax] \"{layer.name}\" wants alpha hit testing, but " +
                                     $"\"{image.sprite?.name}\" has no Read/Write Enabled - tick it in the " +
                                     "texture's import settings, or set Alpha Hit Threshold back to 0.", image);
            }
        }
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
