using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public enum AnimationType
{
    NoTransition,
    ScaleUpFromMiddle,
    ScaleDownFromMiddle,
    SlideInLeft,
    SlideOutLeft,
    SlideInRight,
    SlideOutRight,
    SlideInUp,
    SlideOutUp,
    SlideInDown,
    SlideOutDown,
}

public class UIStackNavigationController
{
    private Transform viewport;
    public Transform Viewport => viewport;

    private Stack controllersStack = new Stack();

    private static bool didRegisterSwipeToBack = false;

    public UIViewController topController => controllersStack.Peek() as UIViewController;

    private UIViewController toRemoveController;

    private AnimationType defaultAnimationType = AnimationType.SlideInRight;

    private static readonly Dictionary<AnimationType, UIControllerAnimation> animations =
        new Dictionary<AnimationType, UIControllerAnimation>();

    private static int sequenceTransitionVersion = 0;
    private int thisTransitionVersion = 0;
    private Sequence transitionAnimationSequence;

    private static int popupTransitionVersion = 0;
    private int thisPopupTransitionVersion = 0;
    private Sequence popupTransitionSequence;

    public UIStackNavigationController(Transform viewportExternal = null)
    {
        viewport = viewportExternal;
        ResetCanvasStackManager();

        if (!didRegisterSwipeToBack)
        {
            // SwipeHandler.OnSwipeToBack += SwipeToBackPop;
            didRegisterSwipeToBack = true;
        }
    }

    public void ResetCanvasStackManager()
    {
        controllersStack = new Stack();
        CreateAnimations();
    }

    private void CreateAnimations()
    {
        animations[AnimationType.SlideInRight] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
        animations[AnimationType.SlideInLeft] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
        animations[AnimationType.SlideInUp] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
        animations[AnimationType.ScaleUpFromMiddle] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
        animations[AnimationType.ScaleDownFromMiddle] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
        animations[AnimationType.SlideOutLeft] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
        animations[AnimationType.SlideOutRight] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
        animations[AnimationType.SlideOutDown] = new UIControllerAnimation { duration = 0.3f, delay = 0f };
    }

    public void Push(UIViewController controller, float delay = 0,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        AddControllersViewToCanvas(controller.view);
        if (controllersStack.Count == 0)
            AddFirstController(controller, delay, animationType);
        else
            AddController(controller, delay, animationType);

        controllersStack.Push(controller);
    }

    public void SwipeToBackPop()
    {
        if (controllersStack.Count > 1)
            Pop(0f);
    }

    public void Pop(float delay = 0)
    {
        PopController(delay);
    }

    public void PresentPopup(
        UIViewController controller,
        float delay = 0,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        AnimatePopupUsingTransitions(controller, isAppearing: true, extraDelay: delay, animationType: animationType);
    }

    public void RemovePopup(
        UIViewController controller,
        float delay = 0,
        Action onComplete = null,
        AnimationType animationType = AnimationType.ScaleDownFromMiddle)
    {
        AnimatePopupUsingTransitions(controller, isAppearing: false, extraDelay: delay, animationType: animationType, onComplete: onComplete);
    }

    private void AddControllersViewToCanvas(GameObject panel)
    {
        panel.transform.SetAsLastSibling();
    }

    public void PopController(float delay, AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        if (controllersStack.Count <= 0)
        {
            Debug.LogWarning("Nothing to pop!!!");
            return;
        }

        toRemoveController = controllersStack.Pop() as UIViewController;

        if (controllersStack.Count == 0)
            RemoveLastController(toRemoveController, delay, animationType);
        else
            RemoveController(toRemoveController, delay, animationType);
    }

    private void RemoveLastController(UIViewController controller, float delay,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        TransitionFromOneControllerToOther(controller, null, animationType, delay);
    }

    private void RemoveController(UIViewController controller, float delay,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        TransitionFromOneControllerToOther(controller, topController, animationType, delay);
    }

    private void AddFirstController(UIViewController controller, float delay,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        TransitionFromOneControllerToOther(null, controller, animationType, delay);
    }

    private void AddController(UIViewController controller, float delay,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        TransitionFromOneControllerToOther(topController, controller, animationType, delay);
    }

    public void TransitionFromOneControllerToOther(
    UIViewController oldController, UIViewController newController,
    AnimationType animationType, float extraDelay = 0f)
{
    var createdOldTemp = false;
    var createdNewTemp = false;

    if (oldController == null)
    {
        oldController = new UIViewController(this)
        { view = new GameObject("Temp_Old", typeof(RectTransform)) };
        if (viewport) oldController.view.transform.SetParent(viewport, false);
        oldController.view.SetActive(false);
        createdOldTemp = true;
    }

    if (newController == null)
    {
        newController = new UIViewController(this)
        { view = new GameObject("Temp_New", typeof(RectTransform)) };
        if (viewport) newController.view.transform.SetParent(viewport, false);
        newController.view.SetActive(false);
        createdNewTemp = true;
    }

    oldController.ViewWillDisappear();
    newController.ViewWillAppear();

    if (!animations.TryGetValue(animationType, out var timing))
    {
        if (!animations.TryGetValue(defaultAnimationType, out timing))
            timing = new UIControllerAnimation();
    }

    var transition = UIControllerTransitionRegistry.GetTransition(animationType)
                    ?? UIControllerTransitionRegistry.GetTransition(defaultAnimationType);

    transitionAnimationSequence?.Kill(true);
    thisTransitionVersion = ++sequenceTransitionVersion;

    var seq = transition.Build(oldController, newController, timing.duration, timing.delay + extraDelay);
    transitionAnimationSequence = seq;

    // Local cleanup that only removes temps we created in this call
    void CleanupTemps()
    {
        if (createdOldTemp && oldController?.view) UnityEngine.Object.Destroy(oldController.view);
        if (createdNewTemp && newController?.view) UnityEngine.Object.Destroy(newController.view);
    }

    seq.OnComplete(() =>
    {
        if (thisTransitionVersion != sequenceTransitionVersion) { CleanupTemps(); return; }

        oldController.view.SetActive(false);
        oldController.ViewDidDisappear();
        newController.ViewDidAppear();

        CleanupTemps();
    });

    // If a newer transition interrupts this one, OnComplete won’t run — clean up temps here too.
    seq.OnKill(() =>
    {
        // Only clean if this sequence was the current one at kill time
        if (thisTransitionVersion != sequenceTransitionVersion) { CleanupTemps(); return; }

        // If we killed ourselves due to replacement, temps are still ours; remove them.
        CleanupTemps();
    });

    seq.Play();
}


    private void AnimatePopupUsingTransitions(
        UIViewController popup,
        bool isAppearing,
        float extraDelay,
        AnimationType animationType,
        Action onComplete = null)
    {
        if (isAppearing)
        {
            popup.view.SetActive(true);
            var rt = popup.view.GetComponent<RectTransform>();
            if (rt != null && (rt.localScale.x == 0f || rt.localScale.y == 0f || rt.localScale.z == 0f))
                rt.localScale = Vector3.one;
            popup.ViewWillAppear();
        }
        else
        {
            popup.view.SetActive(true);
            var rt = popup.view.GetComponent<RectTransform>();
            if (rt != null && animationType == AnimationType.ScaleDownFromMiddle &&
                (rt.localScale.x == 0f || rt.localScale.y == 0f || rt.localScale.z == 0f))
            {
                rt.localScale = Vector3.one;
            }

            popup.ViewWillDisappear();
        }

        if (!animations.TryGetValue(animationType, out var timing))
        {
            if (!animations.TryGetValue(defaultAnimationType, out timing))
                timing = new UIControllerAnimation();
        }

        var transition = UIControllerTransitionRegistry.GetTransition(animationType)
                        ?? UIControllerTransitionRegistry.GetTransition(defaultAnimationType);

        popupTransitionSequence?.Kill(true);
        thisPopupTransitionVersion = ++popupTransitionVersion;

        UIViewController oldC = isAppearing ? null : popup;
        UIViewController newC = isAppearing ? popup : null;

        if (oldC == null)
        {
            oldC = new UIViewController(this) { view = new GameObject("Temp_Popup_Old", typeof(RectTransform)) };
            if (viewport) oldC.view.transform.SetParent(viewport, false);
            oldC.view.SetActive(false);
        }

        if (newC == null)
        {
            newC = new UIViewController(this) { view = new GameObject("Temp_Popup_New", typeof(RectTransform)) };
            if (viewport) newC.view.transform.SetParent(viewport, false);
            newC.view.SetActive(false);
        }

        var seq = transition.Build(oldC, newC, timing.duration, timing.delay + extraDelay);
        popupTransitionSequence = seq;

        seq.OnComplete(() =>
        {
            if (thisPopupTransitionVersion != popupTransitionVersion) return;

            if (isAppearing)
            {
                popup.ViewDidAppear();
            }
            else
            {
                popup.view.SetActive(false);
                popup.ViewDidDisappear();
            }

            if (oldC != popup) UnityEngine.Object.Destroy(oldC.view);
            if (newC != popup) UnityEngine.Object.Destroy(newC.view);

            onComplete?.Invoke();
        });

        seq.Play();
    }
}

public class UIControllerAnimation
{
    public float duration = 0.25f;
    public float delay = 0;
}
