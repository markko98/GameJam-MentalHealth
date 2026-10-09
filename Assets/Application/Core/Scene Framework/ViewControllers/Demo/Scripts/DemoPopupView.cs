using UnityEngine;
using UnityEngine.UI;

public class DemoPopupView : UIViewController
{
    private readonly bool isPopup;
    private const string CloseButtonLabel = "CloseButton";
    private Button closeButton;
    
    public DemoPopupView(bool isPopup, string resourcePath, Transform viewport, UIStackNavigationController controller) : base(controller)
    {
        view = Object.Instantiate(Resources.Load(resourcePath), viewport, false ) as GameObject;
        this.isPopup = isPopup;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        closeButton = GetButtonWithName("CloseButton");
    }

    public override void ViewWillAppear()
    {
        base.ViewWillAppear();
        closeButton.onClick.AddListener(Close);
    }

    public override void ViewWillDisappear()
    {
        base.ViewWillDisappear();
        closeButton.onClick.RemoveListener(Close);
    }

    private void Close()
    {
        if (isPopup)
        {
            RemovePopUp();
        }
        else
        {
            RemoveView();
        }
    }
}