using UnityEngine;

// A page shown by TabHost.
public class TabBaseView : UIViewController
{
    public TabBaseView(GameObject view, UIStackNavigationController controller) : base(view, controller)
    {
    }

    public TabBaseView(UIStackNavigationController controller) : base(controller)
    {
    }

    // True while something is open on top of this tab (a sheet, a popup) that a swipe between tabs would pull away.
    public virtual bool BlocksTabSwipe => false;

    public virtual void OnTabSelected()
    {
    }

    public virtual void OnTabDeselected()
    {
    }

    public virtual void Cleanup()
    {
    }
}
