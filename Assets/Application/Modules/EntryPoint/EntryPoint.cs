using UnityEngine;

public class EntryPoint : AppDelegate
{
    public override void ApplicationStarted()
    {
        base.ApplicationStarted();
        MonologueService.ClearHistory();
        LoadDefaultSceneAsync();
    }
    private void LoadDefaultSceneAsync()
    {
        USceneController sceneController = new GameplayController();
        UNavigationController.SetRootViewController(sceneController,false);
    }
}
