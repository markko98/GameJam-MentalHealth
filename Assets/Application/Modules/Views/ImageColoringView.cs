using UnityEngine;

public class ImageColoringView : UIViewController
{
    private ImageColoringViewOutlet outlet;
    private readonly Sprite sprite;


    public ImageColoringView(Sprite sprite, string resourcePath, Transform viewport, UIStackNavigationController controller) : base(controller)
    {
        this.sprite = sprite;
        view = Object.Instantiate(Resources.Load(resourcePath), viewport, false ) as GameObject;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        outlet = view.GetComponentInChildren<ImageColoringViewOutlet>();
        outlet.image.sprite = sprite;
    }

    public override void ViewWillAppear()
    {
        base.ViewWillAppear();
        outlet.closeButton.Register(Close);
    }

    public override void ViewWillDisappear()
    {
        base.ViewWillDisappear();
        outlet.closeButton.Unregister(Close);
    }

    private void Close()
    {
        RemoveView();
    }
}