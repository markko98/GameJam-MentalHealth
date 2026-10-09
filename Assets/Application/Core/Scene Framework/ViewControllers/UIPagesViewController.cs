using System.Collections.Generic;
using UnityEngine;

public class UIPagesViewController 
{
    private List<UIViewController> pages = new List<UIViewController>();
    private int currentIndex = -1;
    private readonly UIStackNavigationController stackController;

    public int CurrentIndex => currentIndex;

    public UIViewController CurrentPage => 
        currentIndex >= 0 && currentIndex < pages.Count ? pages[currentIndex] : null;
    private readonly Dictionary<int, AnimationType> forwardAnim = new();
    private readonly Dictionary<int, AnimationType> backwardAnim = new();

    public UIPagesViewController(UIStackNavigationController controller)
    {
        this.stackController = controller;
    }
    public void SetPageAnimation(int pageIndex, AnimationType forward, AnimationType backward)
    {
        forwardAnim[pageIndex]  = forward;
        backwardAnim[pageIndex] = backward;
    }
    public void SetPages(List<UIViewController> viewControllers, int startIndex = 0)
    {
        pages = viewControllers ?? new List<UIViewController>();
        if (pages.Count == 0) return;

        currentIndex = Mathf.Clamp(startIndex, 0, pages.Count - 1);
        stackController.Push(pages[currentIndex]);
    }

    public void GoToPage(int index)
    {
        if (index < 0 || index >= pages.Count || index == currentIndex) return;

        var direction = index > currentIndex;
        var fromPage = pages[currentIndex];
        var toPage = pages[index];

        var anim = direction
            ? (forwardAnim.GetValueOrDefault(index, AnimationType.SlideInRight))
            : (backwardAnim.GetValueOrDefault(index, AnimationType.SlideInLeft));

        stackController.TransitionFromOneControllerToOther(fromPage, toPage, anim);
        toPage.ViewDidAppear();
        currentIndex = index;
    }

    public void NextPage()
    {
        if (currentIndex + 1 < pages.Count)
        {
            GoToPage(currentIndex + 1);
        }
    }

    public void PreviousPage()
    {
        if (currentIndex - 1 >= 0)
        {
            GoToPage(currentIndex - 1);
        }
    }

    public void ReloadCurrentPage()
    {
        if (CurrentPage != null)
        {
            stackController.TransitionFromOneControllerToOther(
                CurrentPage,
                CurrentPage,
                AnimationType.SlideInRight
            );
        }
    }
}
