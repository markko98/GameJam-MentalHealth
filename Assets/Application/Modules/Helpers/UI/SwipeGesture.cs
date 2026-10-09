using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Turns a quick horizontal or vertical flick over one area of the UI into a direction. Reusable on any screen.
/// Reads the pointer, not drag events — the same reason as TapToContinue.
public sealed class SwipeGesture : MonoBehaviour
{
    [Tooltip("How far the finger must travel, as a share of the screen's width (horizontal) or height (vertical).")]
    [SerializeField] private float minDistance01 = 0.12f;

    [Tooltip("Longest a press may last and still be a swipe, in seconds.")]
    [SerializeField] private float maxSeconds = 0.6f;

    [Tooltip("How many times longer the main axis must be than the other one.")]
    [SerializeField] private float axisDominance = 2f;

    [Tooltip("Report left/right swipes.")]
    [SerializeField] private bool horizontal = true;

    [Tooltip("Report up/down swipes.")]
    [SerializeField] private bool vertical;

    /// <summary>Fired once per recognised swipe, with the direction the finger travelled.</summary>
    public event Action<Orientation> OnSwipe;

    /// <summary>Optional veto checked when a press begins, for state the hierarchy does not show.</summary>
    public Func<bool> CanStart;

    private static readonly List<RaycastResult> Hits = new();

    private static Vector2 LastPressPosition;

    /// <summary>Whether the current (or just released) press has travelled far enough to be a drag rather than a tap.</summary>
    public static bool PressMovedBeyondTap()
    {
        var pointer = Pointer.current;
        if (pointer == null) return false;

        var threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10;
        return (pointer.position.ReadValue() - LastPressPosition).sqrMagnitude > threshold * threshold;
    }

    private bool pressValid;
    private Vector2 pressPosition;
    private float pressTime;

    /// <summary>Puts a swipe gesture on area (or returns the one already there).</summary>
    public static SwipeGesture Attach(GameObject area, bool horizontal = true, bool vertical = false,
        bool catchGaps = true)
    {
        if (area == null || area.transform as RectTransform == null) return null;

        var gesture = area.GetComponent<SwipeGesture>();
        if (gesture == null) gesture = area.AddComponent<SwipeGesture>();

        gesture.horizontal = horizontal;
        gesture.vertical = vertical;
        if (catchGaps) gesture.EnsureCatcher();

        return gesture;
    }

    private void OnDisable() => pressValid = false;

    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            pressPosition = pointer.position.ReadValue();
            LastPressPosition = pressPosition;
            pressTime = Time.unscaledTime;
            pressValid = (CanStart == null || CanStart()) && StartsOnThisArea(pressPosition);
        }

        if (!pointer.press.wasReleasedThisFrame || !pressValid) return;
        pressValid = false;

        if (Time.unscaledTime - pressTime > maxSeconds) return;

        var delta = pointer.position.ReadValue() - pressPosition;
        var ax = Mathf.Abs(delta.x);
        var ay = Mathf.Abs(delta.y);

        if (horizontal && ax >= minDistance01 * Screen.width && ax >= axisDominance * ay)
        {
            OnSwipe?.Invoke(delta.x < 0 ? Orientation.Left : Orientation.Right);
        }
        else if (vertical && ay >= minDistance01 * Screen.height && ay >= axisDominance * ax)
        {
            OnSwipe?.Invoke(delta.y < 0 ? Orientation.Down : Orientation.Up);
        }
    }

    private bool StartsOnThisArea(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        Hits.Clear();
        eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screenPosition }, Hits);
        if (Hits.Count == 0) return false;

        var top = Hits[0].gameObject.transform;
        Hits.Clear();

        if (!top.IsChildOf(transform)) return false;

        // Walk up to the area: the first thing on the way that moves along our axis owns the gesture.
        for (var t = top; t != null && t != transform.parent; t = t.parent)
        {
            if (t.GetComponent<SwipeGestureBlocker>() != null) return false;

            // Any control — a button, a toggle, a slider — owns a press that starts on it.
            var selectable = t.GetComponent<Selectable>();
            if (selectable != null && selectable.IsInteractable() && selectable.isActiveAndEnabled &&
                t.GetComponent<SwipeGesturePassThrough>() == null) return false;

            var scroll = t.GetComponent<ScrollRect>();
            if (scroll != null && scroll.isActiveAndEnabled && ScrollsAlongOurAxis(scroll)) return false;
        }

        return true;
    }

    /// <summary>Whether a scroll view owns gestures along the axis this swipe reads.</summary>
    private bool ScrollsAlongOurAxis(ScrollRect scroll)
    {
        return (horizontal && scroll.horizontal && (!scroll.vertical || Overflows(scroll, horizontalAxis: true))) ||
               (vertical && scroll.vertical && (!scroll.horizontal || Overflows(scroll, horizontalAxis: false)));
    }

    private static bool Overflows(ScrollRect scroll, bool horizontalAxis)
    {
        if (scroll.content == null) return false;

        var viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
        if (viewport == null) return false;

        return horizontalAxis
            ? scroll.content.rect.width > viewport.rect.width + 1f
            : scroll.content.rect.height > viewport.rect.height + 1f;
    }

    private const string CatcherName = "SwipeGestureCatcher";

    private void EnsureCatcher()
    {
        if (transform.Find(CatcherName) != null) return;

        var catcher = new GameObject(CatcherName, typeof(RectTransform), typeof(Image));
        catcher.layer = gameObject.layer;

        var rect = (RectTransform)catcher.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();

        var image = catcher.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
    }
}

