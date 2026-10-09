using System.Collections.Generic;

public static class UIControllerTransitionRegistry
{
    private static readonly Dictionary<AnimationType, IUIControllerTransition> transitionsMap
        = new Dictionary<AnimationType, IUIControllerTransition>
        {
            { AnimationType.NoTransition, new NoTransition() },
            { AnimationType.SlideInRight, new SlideInRightTransition() },
            { AnimationType.SlideInLeft,  new SlideInLeftTransition() },
            { AnimationType.SlideInUp,      new SlideInUpTransition() },
            { AnimationType.SlideOutUp,      new SlideOutUpTransition() },
            { AnimationType.ScaleUpFromMiddle,   new ScaleUpFromMiddleTransition() },
            { AnimationType.ScaleDownFromMiddle, new ScaleDownFromMiddleTransition() },
            { AnimationType.SlideInDown, new SlideInDownTransition() },
            { AnimationType.SlideOutDown, new SlideOutDownTransition() },
            { AnimationType.SlideOutLeft, new SlideOutLeftTransition() },
            { AnimationType.SlideOutRight, new SlideOutRightTransition() },
        };

    public static IUIControllerTransition GetTransition(AnimationType type)
        => transitionsMap.TryGetValue(type, out var t) ? t : new SlideInLeftTransition();
}