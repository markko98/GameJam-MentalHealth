using System;
using UnityEngine;

public class SwipeHandler : MonoBehaviour
{
    private Vector2 fingerDownPosition;
    private Vector2 fingerUpPosition;
    private Vector2 swipeStartPostion;

    [SerializeField]
    private bool detectSwipeOnlyAfterRelease = true;

    [SerializeField]
    private float minDistanceForSwipe = 20f;

    [SerializeField]
    private float maxDistanceFromEdgeForSwipeToBack = 40f;

    private bool didSwipeToBackInThisTouch = false;

    public static event Action<SwipeData> OnSwipe = delegate { };
    public static event Action OnSwipeToBack = delegate { };

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        //touch
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                fingerUpPosition = touch.position;
                fingerDownPosition = touch.position;
                swipeStartPostion = touch.position;
            }

            if (!detectSwipeOnlyAfterRelease && touch.phase == TouchPhase.Moved)
            {
                fingerDownPosition = touch.position;
                DetectSwipe();
            }

            if (touch.phase == TouchPhase.Ended)
            {
                fingerDownPosition = touch.position;
                DetectSwipe();

                didSwipeToBackInThisTouch = false;
            }
        }

        //mouse
        //if (Input.GetMouseButtonDown(0))
        //{
        //    fingerUpPosition = Input.mousePosition;
        //    fingerDownPosition = Input.mousePosition;
        //    swipeStartPostion = Input.mousePosition;
        //    return;
        //}

        //if (Input.GetMouseButtonUp(0))
        //{
        //    fingerDownPosition = Input.mousePosition;
        //    DetectSwipe();
        //    return;
        //}

        //if (Input.GetMouseButton(0) && !detectSwipeOnlyAfterRelease)
        //{
        //    fingerDownPosition = Input.mousePosition;
        //    DetectSwipe();
        //    return;
        //}
    }

    private void DetectSwipe()
    {
        if (SwipeDistanceCheckMet())
        {
            if (IsVerticalSwipe())
            {
                var direction = fingerDownPosition.y - fingerUpPosition.y > 0 ? Orientation.Up : Orientation.Down;
                SendSwipe(direction);
            }
            else
            {
                var direction = fingerDownPosition.x - fingerUpPosition.x > 0 ? Orientation.Right : Orientation.Left;
                SendSwipe(direction);

                if (swipeStartPostion.x < maxDistanceFromEdgeForSwipeToBack && !didSwipeToBackInThisTouch)
                {
                    Debug.Log("Swipe to back");
                    OnSwipeToBack.Invoke();
                    didSwipeToBackInThisTouch = true;
                }
            }

            fingerUpPosition = fingerDownPosition;
        }
    }

    private bool IsVerticalSwipe()
    {
        return VerticalMovementDistance() > HorizontalMovementDistance();
    }

    private bool SwipeDistanceCheckMet()
    {
        return VerticalMovementDistance() > minDistanceForSwipe || HorizontalMovementDistance() > minDistanceForSwipe;
    }

    private float VerticalMovementDistance()
    {
        return Mathf.Abs(fingerDownPosition.y - fingerUpPosition.y);
    }

    private float HorizontalMovementDistance()
    {
        return Mathf.Abs(fingerDownPosition.x - fingerUpPosition.x);
    }

    private void SendSwipe(Orientation direction)
    {
        //UNCOMMENT IF NEEDED, removed to reduce overhead 
        //SwipeData swipeData = new SwipeData()
        //{
        //    Direction = direction,
        //    StartPosition = fingerDownPosition,
        //    EndPosition = fingerUpPosition
        //};
        //OnSwipe(swipeData);
    }
}

public struct SwipeData
{
    public Vector2 StartPosition;
    public Vector2 EndPosition;
    public Orientation Direction;
}

public enum Orientation
{
    Up, Down, Left, Right 
}