using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Owns the current GameMode and applies what each mode looks like: zoom and parallax strength.
// What is clickable per mode is up to each GameModeInteractable. Change the mode from anywhere with
// GameModeHandler.Set(mode), or by raising ModeChangedEvent directly - both end up here.
[DefaultExecutionOrder(-100)]
public class GameModeHandler : MonoBehaviour
{
    [Serializable]
    private class ModeSettings
    {
        public GameMode mode;
        [Tooltip("Zoomed onto, centred on screen. Empty = the centre of the scene.")]
        public RectTransform zoomFocus;
        [Tooltip("1 = the whole scene, higher = closer.")]
        [Min(0.01f)] public float zoom = 1f;
        [Tooltip("UIParallax strength in this mode, 0 = layers stand still.")]
        [Range(0f, 1f)] public float parallaxStrength = 1f;
    }

    /// <summary>The mode the game is in right now; GameMode.Other until a handler has woken up.</summary>
    public static GameMode Current { get; private set; } = GameMode.Other;

    [SerializeField] private GameMode initialMode = GameMode.Break;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private UIParallax parallax;
    [Tooltip("Seconds a mode change takes to zoom and settle.")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.8f;
    [SerializeField] private List<ModeSettings> modes = new List<ModeSettings>
    {
        new ModeSettings { mode = GameMode.Break, zoom = 1f, parallaxStrength = 1f },
        new ModeSettings { mode = GameMode.Task, zoom = 2f, parallaxStrength = 0.15f },
    };

    private EventBinding<ModeChangedEvent> modeChangedBinding;
    private Tween parallaxTween;

    /// <summary>Switches the game to <paramref name="mode"/>; does nothing if it is already in it.</summary>
    public static void Set(GameMode mode)
    {
        if (mode == Current) return;

        UEventBus<ModeChangedEvent>.Raise(new ModeChangedEvent(mode));
    }

    private void Awake()
    {
        // Set before any GameModeInteractable enables, so they start in the right state.
        Current = initialMode;
        modeChangedBinding = new EventBinding<ModeChangedEvent>(OnModeChanged);
        UEventBus<ModeChangedEvent>.Register(modeChangedBinding);
    }

    // Start, not Awake: the parallax layers have to be built and laid out to zoom onto them.
    private void Start() => Apply(Current, 0f);

    private void OnDestroy()
    {
        UEventBus<ModeChangedEvent>.Deregister(modeChangedBinding);
        parallaxTween?.Kill();
    }

    private void OnModeChanged(ModeChangedEvent data)
    {
        if (data.mode == Current) return;

        Current = data.mode;
        Apply(data.mode, transitionDuration);
    }

    private void Apply(GameMode mode, float duration)
    {
        var settings = modes.Find(entry => entry.mode == mode);
        if (settings == null) return;

        if (cameraController != null) cameraController.ZoomTo(settings.zoomFocus, settings.zoom, duration);
        if (parallax != null) TweenParallax(settings.parallaxStrength, duration);
    }

    private void TweenParallax(float strength, float duration)
    {
        parallaxTween?.Kill();

        if (duration <= 0f)
        {
            parallax.Strength = strength;
            return;
        }

        parallaxTween = DOTween.To(() => parallax.Strength, value => parallax.Strength = value, strength, duration)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
