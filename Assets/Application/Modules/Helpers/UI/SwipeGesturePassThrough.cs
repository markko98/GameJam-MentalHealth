using UnityEngine;

/// Marks a control whose press may start a SwipeGesture anyway — a large card that is mostly surface, where
/// refusing every swipe that lands on it would leave almost nowhere to swipe from. Its click handler should check
/// SwipeGesture.PressMovedBeyondTap so a swipe does not also click it.
public sealed class SwipeGesturePassThrough : MonoBehaviour
{
}
