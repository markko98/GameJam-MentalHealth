using System.Collections.Generic;
using JeffGrawAssets.FlexibleUI;
using UnityEngine;
using Interpolation = JeffGrawAssets.FlexibleUI.ProceduralProperties.InterpolationType;

// Almond shaped eyelids on one FlexibleImage, animated by its own script-driven states.
// Two black quads each have a round SDF cutout; what stays visible is where the holes overlap,
// a lens that pinches at the sides as the circles slide apart. The object is stretched on X so
// the lens is wider and flatter than the circles (FlexibleImage caps the cutout radius at ~1024).
//
//   await outlet.eyelids.Blink();
//   await outlet.eyelids.SlowBlink();
//   await outlet.eyelids.Close(); ... await outlet.eyelids.Open();
[RequireComponent(typeof(FlexibleImage))]
public class EyelidsAnimation : MonoBehaviour
{
    private const int OpenState = 0;
    private const int ClosedState = 1;
    private const int SequenceStateA = 2;
    private const int SequenceStateB = 3;
    private const float Radius = 1000f;

    [SerializeField] private Color color = Color.black;
    [SerializeField] private float softness = 40f;
    [Tooltip("How round the lid edges are; lower is flatter.")]
    [SerializeField, Range(0.1f, 1f)] private float curvature = 0.5f;

    [Header("Blink")]
    [SerializeField] private float blinkClose = 0.08f;
    [SerializeField] private float blinkOpen = 0.15f;

    [Header("Slow blink")]
    [SerializeField] private float slowClose = 1.2f;
    [SerializeField] private float slowHold = 0.4f;
    [SerializeField] private float slowOpen = 1.5f;

    [Header("Close / Open")]
    [SerializeField] private float closeDuration = 1f;
    [SerializeField] private float openDuration = 1f;

    private FlexibleImage image;
    private readonly List<(ProceduralProperties frame, float side, bool open)> frames = new();
    private int sequenceState = SequenceStateB;

    private void Awake()
    {
        image = GetComponent<FlexibleImage>();
        image.raycastTarget = false;
        image.animationMode = FlexibleImage.AnimationStateDrivenBy.Script;
        image.scriptDrivenAnimationState = OpenState;
        // Single mode draws only the primary quad
        image.DataMode = FlexibleImage.QuadDataMode.Multiple;
        Build();
    }

    public Awaitable Blink() => PlaySequence(blinkClose, 0f, blinkOpen);

    public Awaitable SlowBlink() => PlaySequence(slowClose, slowHold, slowOpen);

    public Awaitable Close(float? duration = null)
    {
        var seconds = Duration(duration ?? closeDuration);
        foreach (var quad in image.ActiveQuadDataContainer)
            quad.proceduralAnimationStates[ClosedState].proceduralProperties[0].duration = seconds;

        return Play(ClosedState, seconds);
    }

    public Awaitable Open(float? duration = null)
    {
        var seconds = Duration(duration ?? openDuration);
        foreach (var quad in image.ActiveQuadDataContainer)
            quad.DefaultProceduralProps.duration = seconds;

        return Play(OpenState, seconds);
    }

    // Selecting the state that is already playing does not restart it, so sequences
    // alternate between two identical slots and every call starts from the current pose.
    private Awaitable PlaySequence(float close, float hold, float open)
    {
        sequenceState = sequenceState == SequenceStateA ? SequenceStateB : SequenceStateA;

        close = Duration(close);
        hold = Duration(hold);
        open = Duration(open);

        foreach (var quad in image.ActiveQuadDataContainer)
        {
            var substates = quad.proceduralAnimationStates[sequenceState].proceduralProperties;
            substates[0].duration = close;
            substates[1].duration = hold;
            substates[2].duration = open;
        }

        return Play(sequenceState, close + hold + open);
    }

    private Awaitable Play(int state, float seconds)
    {
        image.scriptDrivenAnimationState = state;
        return Awaitable.WaitForSecondsAsync(seconds);
    }

    private void Build()
    {
        var quads = image.ActiveQuadDataContainer;
        while (quads.Count > 2) quads.RemoveQuadData(quads.Count - 1);
        while (quads.Count < 2) quads.AddQuadData();

        frames.Clear();
        BuildLid(quads[0], 1f);
        BuildLid(quads[1], -1f);
        UpdateLayout();
    }

    // side 1: hole sits below the centre, its top arc is the upper lid; side -1: the lower lid
    private void BuildLid(QuadData quad, float side)
    {
        quad.name = side > 0f ? "TopLid" : "BottomLid";
        quad.AnchorMin = Vector2.zero;
        quad.AnchorMax = Vector2.one;
        quad.AnchoredPosition = Vector2.zero;
        quad.Pivot = new Vector2(0.5f, 0.5f);
        quad.SizeDelta = Vector2.one * softness * 4f;

        quad.EnableCutout();
        quad.Cutout = QuadData.CutoutType.SDF;
        quad.SDFCutoutUsesAnchors = false;
        quad.SDFCutoutPositionIsAbsolute = true;
        quad.SDFCutoutSizeIsAbsolute = true;
        quad.SdfCutoutChamferNormalize = true;
        quad.SDFCutoutIsSquircle = false;

        var open = quad.DefaultProceduralProps;
        open.softness = softness;
        open.interpolationType = Interpolation.SineEaseInOut;
        open.duration = Duration(openDuration);
        open.cornerChamfer = Vector4.zero;
        for (var i = 0; i < open.primaryColors.Length; i++) open.primaryColors[i] = color;
        open.sdfCutoutSize = Vector2.one * Radius * 2f;
        open.sdfCutoutChamfer = Vector4.one * Radius;
        open.sdfCutoutConcavity = Vector4.zero;
        open.sdfCutoutRotation = 0f;

        var states = quad.proceduralAnimationStates;
        states[OpenState].proceduralProperties.RemoveRange(1, states[OpenState].proceduralProperties.Count - 1);
        SetSubstates(states[ClosedState], false);
        SetSubstates(states[SequenceStateA], false, false, true);
        SetSubstates(states[SequenceStateB], false, false, true);
        frames.Add((open, side, true));

        void SetSubstates(ProceduralAnimationState state, params bool[] openFlags)
        {
            state.proceduralProperties.Clear();
            foreach (var isOpen in openFlags)
            {
                var frame = new ProceduralProperties(open);
                state.proceduralProperties.Add(frame);
                frames.Add((frame, side, isOpen));
            }
            state.playbackType = ProceduralAnimationState.PlaybackType.Once;
            state.unwindToIdx = -1;
        }
    }

    private void UpdateLayout()
    {
        var rect = ((RectTransform)transform).rect;
        if (rect.width <= 0f || rect.height <= 0f) return;

        // keep the soft edge fully off screen when open and fully overlapping when closed
        var margin = softness * 2f;
        var halfHeight = rect.height * 0.5f + margin;
        var widestFit = Mathf.Sqrt(Mathf.Max(0f, Radius * Radius - halfHeight * halfHeight)) - margin;
        var stretch = Mathf.Max(1f / curvature, rect.width * 0.5f / Mathf.Max(1f, widestFit));
        transform.localScale = new Vector3(stretch, 1f, 1f);

        var halfWidth = rect.width * 0.5f / stretch + margin;
        var openDistance = Mathf.Max(0f, Mathf.Sqrt(Mathf.Max(0f, Radius * Radius - halfWidth * halfWidth)) - halfHeight);
        var closedDistance = Radius + margin;

        foreach (var (frame, side, open) in frames)
            frame.sdfCutoutPosition = new Vector2(0f, -side * (open ? openDistance : closedDistance));

        image.SetVerticesDirty();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (image != null && frames.Count > 0) UpdateLayout();
    }

    private static float Duration(float seconds) => Mathf.Max(0.001f, seconds);
}
