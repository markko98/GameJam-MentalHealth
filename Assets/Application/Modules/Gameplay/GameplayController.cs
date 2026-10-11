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
    private EventBinding<GameOverEvent> gameOverBinding;
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
        GameModeInteractable.Allow(InteractionGroup.Desktop);

        GameModeHandler.Set(GameMode.Task);
        outlet.playButton.Register(StartGame);
        // StartDemoMonologue();

        outlet.eyelids.Close(0);
        DelayedExecutionManager.ExecuteActionAfterDelay(1000, OpenEyelid).disposeBy(disposeBag);
        GameTicker.SharedInstance.Update += DebugModeKeys;
    }

    private void OpenEyelid()
    {
        outlet.eyelids.Open(3);
    }

    private void DebugModeKeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) GameModeHandler.Set(GameMode.Break);
        else if (keyboard.digit2Key.wasPressedThisFrame) GameModeHandler.Set(GameMode.Task);
        else if (keyboard.digit3Key.wasPressedThisFrame) StartBurnoutMonologue();
        else if (keyboard.digit4Key.wasPressedThisFrame) outlet.eyelids.Blink();
        else if (keyboard.digit6Key.wasPressedThisFrame) outlet.burnoutMeter.IncreaseBurnout(20f);
        else if (keyboard.digit7Key.wasPressedThisFrame) outlet.burnoutMeter.DecreaseBurnout(20f);
        else if (keyboard.digit8Key.wasPressedThisFrame) GameModeInteractable.Allow(InteractionGroup.Items);
        else if (keyboard.digit9Key.wasPressedThisFrame) GameModeInteractable.Allow(InteractionGroup.Desktop);
    }
    private void SubscribeToEvents()
    {
        modeChangedEvent = new EventBinding<ModeChangedEvent>(OnModeChanged);
        UEventBus<ModeChangedEvent>.Register(modeChangedEvent);
        itemClickedBinding = new EventBinding<ItemClickedEvent>(OnItemClicked);
        UEventBus<ItemClickedEvent>.Register(itemClickedBinding);
        gameOverBinding = new EventBinding<GameOverEvent>(OnGameOver);
        UEventBus<GameOverEvent>.Register(gameOverBinding);
    }

    private void OnGameOver(GameOverEvent args)
    {
        outlet.eyelids.Close(3);
    }

    private void OnModeChanged(ModeChangedEvent mode)
    {
        // zoom, parallax and what is clickable are handled by GameModeHandler / GameModeInteractable;
        GameModeHandler.Set(mode.mode);
        ToggleClickableElements(mode.mode);
    }

    private void ToggleClickableElements(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Break:
                GameModeInteractable.Allow(InteractionGroup.Items);
                break;
            case GameMode.Task:
                GameModeInteractable.Allow(InteractionGroup.Desktop);
                break;
            case GameMode.Other:
                GameModeInteractable.Allow(InteractionGroup.All);
                break;
        }
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
        ShowImageColoringView(GetSpriteForImage("Item_Polaroid_Friends"));
    }
    private void ShowImageColoringView(Sprite sprite)
    {
        if (coloringView != null && coloringView.IsActive)
        {
            coloringView.RemoveView(0f, null, AnimationType.ScaleDownFromMiddle);
            return;
        }
        coloringView ??= new ImageColoringView(sprite, "ImageColoringView", outlet.canvas.transform, uiStackController);
        coloringView.PresentView(0.3f, AnimationType.ScaleUpFromMiddle);
    }

    private Sprite GetSpriteForImage(string id)
    {
        switch (id)
        {
            case "Item_Maca":
                return SpriteProvider.Sprites.item_Cat_Sprite;
            case "Item_Polaroid_Friends":
                return SpriteProvider.Sprites.item_Polaroid_Friends_Sprite;
        }

        return SpriteProvider.Sprites.item_Cat_Sprite;
    }
    
    public override void SceneWillDisappear()
    {
        base.SceneWillDisappear();
        outlet.playButton.Unregister(StartGame);
        MonologueService.Stop(true);
        MonologueService.ClearHistory();
        UEventBus<ItemClickedEvent>.Deregister(itemClickedBinding);
        UEventBus<ModeChangedEvent>.Deregister(modeChangedEvent);
        UEventBus<GameOverEvent>.Deregister(gameOverBinding);
        GameTicker.SharedInstance.Update -= DebugModeKeys;
        disposeBag.Dispose();
    }
}
