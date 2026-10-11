using UnityEngine;

public class ImageColoringView : UIViewController
{
    private ImageColoringViewOutlet outlet;
    private ImageColoring coloring;
    private Sprite sprite;
    private DisposeBag disposeBag;
    private bool doneRaised;


    public ImageColoringView(Sprite sprite, string resourcePath, Transform viewport, UIStackNavigationController controller) : base(controller)
    {
        this.sprite = sprite;
        view = Object.Instantiate(Resources.Load(resourcePath), viewport, false ) as GameObject;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        outlet = view.GetComponentInChildren<ImageColoringViewOutlet>();
        if (!outlet.image.TryGetComponent(out coloring))
            coloring = outlet.image.gameObject.AddComponent<ImageColoring>();
    }

    // The view is reused (hidden, not destroyed) between openings - set the next picture before PresentView.
    public void SetSprite(Sprite newSprite) => sprite = newSprite;

    public override void ViewWillAppear()
    {
        base.ViewWillAppear();
        disposeBag = new DisposeBag();
        doneRaised = false;
        coloring.Setup(sprite);
        outlet.closeButton.Register(Close);
        coloring.Completed += OnColoringCompleted;
    }

    public override void ViewWillDisappear()
    {
        base.ViewWillDisappear();
        disposeBag.Dispose();
        outlet.closeButton.Unregister(Close);
        coloring.Completed -= OnColoringCompleted;
    }

    // Raised once the view is fully closed, also when the player closes it before the auto-close.
    public override void ViewDidDisappear()
    {
        base.ViewDidDisappear();
        if (!coloring.IsCompleted || doneRaised) return;

        doneRaised = true;
        UEventBus<ImageColoringDoneEvent>.Raise(new ImageColoringDoneEvent(sprite));
    }

    private void OnColoringCompleted()
    {
        DelayedExecutionManager.ExecuteActionAfterDelay(1000, Close).disposeBy(disposeBag);
    }

    private void Close()
    {
        RemoveView();
    }
}
