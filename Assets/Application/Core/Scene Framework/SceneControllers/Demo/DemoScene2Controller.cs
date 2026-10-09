using UnityEngine;

public class DemoScene2Controller : USceneController
{
    private DemoSceneOutlet outlet;
    private UIStackNavigationController uiStackController;
    private DemoPopupView demoPopupView;
    private DemoPopupView demoView2;
    private DemoPopupView demoView3;

    public DemoScene2Controller() : base(DemoSceneNames.Demo2)
    {
    }
    public override void SceneDidLoad()
    {
        base.SceneDidLoad();

        outlet = GameObject.Find("Outlet").GetComponent<DemoSceneOutlet>();
        uiStackController = new UIStackNavigationController(outlet.canvas.transform);
        outlet.nextSceneButton.onClick.AddListener(OpenScene);
        outlet.togglePopupButton.onClick.AddListener(TogglePopup);
        outlet.toggleSlide2Button.onClick.AddListener(ToggleView2);
        outlet.toggleSlide1Button.onClick.AddListener(ToggleView3);
    }

    public override void SceneWillDisappear()
    {
        base.SceneWillDisappear();
        outlet.nextSceneButton.onClick.RemoveListener(OpenScene);
        outlet.togglePopupButton.onClick.RemoveListener(TogglePopup);
        outlet.toggleSlide2Button.onClick.RemoveListener(ToggleView2);
        outlet.toggleSlide1Button.onClick.RemoveListener(ToggleView3);
    }

    private void TogglePopup()
    {
        if (demoPopupView != null && demoPopupView.IsActive)
        {
            demoPopupView.RemovePopUp();
            return;
        }
        demoPopupView ??= new DemoPopupView(true, "TestView1", outlet.canvas.transform, uiStackController);
        demoPopupView.PresentViewAsPopup();
    }
    
    private void ToggleView2()
    {
        if (demoView2 != null && demoView2.IsActive)
        {
            demoView2.RemoveView(0f, null, AnimationType.SlideOutLeft);
            return;
        }
        demoView2 ??= new DemoPopupView(false, "TestView2", outlet.canvas.transform, uiStackController);
        demoView2.PresentView(0f, AnimationType.SlideInRight);
    }
    
    private void ToggleView3()
    {
        if (demoView3 != null && demoView3.IsActive)
        {
            demoView3.RemoveView(1f, null, AnimationType.SlideOutDown);
            return;
        }
        demoView3 ??= new DemoPopupView(false, "TestView3", outlet.canvas.transform, uiStackController);
        demoView3.PresentView(0f, AnimationType.SlideInDown);
    }

    private void OpenScene()
    {
        var nextScene = new DemoScene1Controller();
        PushSceneController(nextScene);
    }

}