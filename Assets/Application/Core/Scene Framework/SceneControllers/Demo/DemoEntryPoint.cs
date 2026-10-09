using UnityEngine;

public class DemoEntryPoint : AppDelegate
{
    private UIStackNavigationController uiStackController;
    private DemoEntryPointOutlet outlet;

    public override void ApplicationStarted()
    {
        base.ApplicationStarted();
        outlet = GameObject.Find("DemoEntryPointOutlet").GetComponent<DemoEntryPointOutlet>();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        uiStackController = new UIStackNavigationController(outlet.canvas.transform);
        LoadDefaultSceneAsync();
    }
    private void LoadDefaultSceneAsync()
    {
        USceneController sceneController = new DemoScene1Controller();
        UNavigationController.SetRootViewController(sceneController,false);
    }
}
