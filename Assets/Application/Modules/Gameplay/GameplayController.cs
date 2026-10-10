using UnityEngine;
using UnityEngine.InputSystem;

public enum GameMode
{
    Break, Task, Other
}
public class GameplayController : USceneController
{
    private GameplayOutlet outlet;
    private UIStackNavigationController uiStackController;
    private ImageColoringView coloringView;
    private EventBinding<ItemClickedEvent> itemClickedBinding;
    private EventBinding<ModeChangedEvent> modeChangedEvent;
    private DisposeBag disposeBag;

    public GameplayController() : base(SceneNames.Gameplay)
    {
    }

    public override void SceneDidLoad()
    {
        base.SceneDidLoad();
        disposeBag = new DisposeBag();
        MonologueService.ClearHistory();
        outlet = GameObject.Find(OutletNames.Gameplay).GetComponent<GameplayOutlet>();
        uiStackController = new UIStackNavigationController(outlet.canvas.transform);
        SubscribeToEvents();
        
        GameModeHandler.Set(GameMode.Task);
        outlet.playButton.Register(StartGame);
        // StartDemoMonologue();

        outlet.eyelids.Close(0);
        DelayedExecutionManager.ExecuteActionAfterDelay(1000, OpenEyelid).disposeBy(disposeBag);
        GameTicker.SharedInstance.Update += DebugModeKeys;
    }

    private void OpenEyelid()
    {
        outlet.eyelids.Open(1);
    }

    private void DebugModeKeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) GameModeHandler.Set(GameMode.Break);
        else if (keyboard.digit2Key.wasPressedThisFrame) GameModeHandler.Set(GameMode.Task);
        else if (keyboard.digit3Key.wasPressedThisFrame) StartBurnoutMonologue();
    }
    private void SubscribeToEvents()
    {
        modeChangedEvent = new EventBinding<ModeChangedEvent>(OnModeChanged);
        UEventBus<ModeChangedEvent>.Register(modeChangedEvent);
        itemClickedBinding = new EventBinding<ItemClickedEvent>(OnItemClicked);
        UEventBus<ItemClickedEvent>.Register(itemClickedBinding);
    }
    private void OnModeChanged(ModeChangedEvent mode)
    {
        // zoom, parallax and what is clickable are handled by GameModeHandler / GameModeInteractable;
        GameModeHandler.Set(mode.mode);
        ToggleClickableElements(mode.mode);
        // gameplay reactions to a mode change (monologues, sounds, timers) go here
    }

    private void ToggleClickableElements(GameMode mode)
    {
        
    }

    private void StartBurnoutMonologue()
    {
        MonologueService.Play(MonologueIds.Burnout_Indicator);
    }

    public override void SceneWillAppear()
    {
        base.SceneWillAppear();
    }

    private void StartGame()
    {
        // turn screen on
        PlayDesktopUnlock();
        // hide buttons
        // enable input
    }
    
    private void PlayDesktopUnlock()
    {
        outlet.desktopUnlock.PlayState(FlexibleState.Highlighted); // otključaj ekran
        outlet.desktopUnlock.raycastTarget = false;
        outlet.playButton.interactable = false;
        outlet.playButton.gameObject.SetActive(false);
    }

    
    private void OnItemClicked(ItemClickedEvent data)
    {
        switch (data.id)
        {
            case "Item_Maca":
                PlayCatView(); break;
            case "Item_Polaroid_Friends":
                PlaySlika1View(); break;
            // case "Monitor":; break;
        }
    }
    
    private void PlayCatView()
    {
        // MonologueService.Play(MonologueIds.ma);
        // show black screen and play purring sound
        Debug.Log("Playing Cat");
    }
    
    private void PlaySlika1View()
    {
        // MonologueService.Play(MonologueIds.ma);
        Debug.Log("Playing Item_Polaroid_Friends");
    }
    private void ShowImageColoringView(Sprite sprite)
    {
        if (coloringView != null && coloringView.IsActive)
        {
            coloringView.RemoveView(0f, null, AnimationType.ScaleDownFromMiddle);
            return;
        }
        coloringView ??= new ImageColoringView(sprite, "ColoringView", outlet.canvas.transform, uiStackController);
        coloringView.PresentView(0f, AnimationType.SlideInRight);
    }

    public override void SceneWillDisappear()
    {
        base.SceneWillDisappear();
        outlet.playButton.Unregister(StartGame);
        MonologueService.Stop(true);
        MonologueService.ClearHistory();
        UEventBus<ItemClickedEvent>.Deregister(itemClickedBinding);
        UEventBus<ModeChangedEvent>.Deregister(modeChangedEvent);
        GameTicker.SharedInstance.Update -= DebugModeKeys;
        disposeBag.Dispose();
    }
}
