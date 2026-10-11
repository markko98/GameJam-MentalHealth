using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public enum GameMode
{
    Break, Task, Other
}
public class GameplayController : USceneController
{
    // Tuned so picking "keep working" in any 2 of the 3 choices ends in game over, and 0-1 never does.
    private const float TaskBurnout = 25f;
    private const float KeepWorkingBurnout = 25f;
    private const float LogoPlaceholderBurnout = 60f; // logo task + keep working, with room for idle relief
    private const float LogoCreativeRelief = 20f;
    private const float BreakBurnoutRelief = 20f;
    private const float IdleBurnoutRelief = 10f;
    private const int IdleDelay = 15000;
    private const string CatVideoPath = "Video macka";

    private GameplayOutlet outlet;
    private UIStackNavigationController uiStackController;
    private ImageColoringView coloringView;
    private MultipleChoiceView multipleChoiceView;
    private EventBinding<ItemClickedEvent> itemClickedBinding;
    private EventBinding<ModeChangedEvent> modeChangedEvent;
    private EventBinding<GameOverEvent> gameOverBinding;
    private EventBinding<MailTaskDoneEvent> mailTaskDoneBinding;
    private EventBinding<ImageColoringDoneEvent> imageColoringDoneBinding;
    private EventBinding<SlackCallAnsweredEvent> slackCallAnsweredBinding;
    private EventBinding<LogoTaskDoneEvent> logoTaskDoneBinding;
    private ClickableItem coloringItem;
    private Action afterBreak;
    private DelayedExecutionTicket idleTicket;
    private bool isGameOver;
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
        
        outlet.eyelids.Close(0);
        GameModeInteractable.Allow(InteractionGroup.None);
        GameModeHandler.Set(GameMode.Task);
        outlet.playButton.Register(StartGame);
        SetTime("08:05");

        DelayedExecutionManager.ExecuteActionAfterDelay(1000, OpenEyelid).disposeBy(disposeBag);
        GameTicker.SharedInstance.Update += DebugModeKeys;
    }

    private void StartInitialMonologue()
    {
        MonologueService.Play(MonologueIds.Burnout_Indicator, ShowMailTask);
    }

    private void ShowMailTask()
    {
        outlet.folderIcon.SetHighlighted(true);
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
        mailTaskDoneBinding = new EventBinding<MailTaskDoneEvent>(OnMailTaskDone);
        UEventBus<MailTaskDoneEvent>.Register(mailTaskDoneBinding);
        imageColoringDoneBinding = new EventBinding<ImageColoringDoneEvent>(OnImageColoringDone);
        UEventBus<ImageColoringDoneEvent>.Register(imageColoringDoneBinding);
        slackCallAnsweredBinding = new EventBinding<SlackCallAnsweredEvent>(OnSlackCallAnswered);
        UEventBus<SlackCallAnsweredEvent>.Register(slackCallAnsweredBinding);
        logoTaskDoneBinding = new EventBinding<LogoTaskDoneEvent>(OnLogoTaskDone);
        UEventBus<LogoTaskDoneEvent>.Register(logoTaskDoneBinding);
    }

    #region Tasks

    
    private void OnMailTaskDone(MailTaskDoneEvent obj)
    {
        Debug.Log("Mail Task Done");
        SetTime("10:00");
        if (ChangeBurnout(TaskBurnout)) return;
        GameModeInteractable.Allow(InteractionGroup.None);
        MonologueService.Play(MonologueIds.AfterMailTask, ShowChoice01);
    }

    private void StartLogoFeedbackTask()
    {
        outlet.desktopUI.HighlightLogoTask();
    }

    // Placeholder logo adds burnout, the creative one relieves it; then a break before the window ending.
    private void OnLogoTaskDone(LogoTaskDoneEvent data)
    {
        SetTime("15:45");
        if (ChangeBurnout(data.creative ? -LogoCreativeRelief : LogoPlaceholderBurnout)) return;
        afterBreak = ShowWindow;
        StartBreak();
    }

    #endregion

    #region Ending

    // Only the window can be clicked now.
    private void ShowWindow()
    {
        GameModeInteractable.Allow(InteractionGroup.Window);
        outlet.window.SetHighlighted(true);
    }

    private void OnWindowClicked()
    {
        GameModeInteractable.Allow(InteractionGroup.None);
        outlet.window.SetHighlighted(false);
        outlet.eyelids.Close(3);
        MonologueService.Play(MonologueIds.Monologue_Demo, ResetScene); // TODO: game complete monologue
    }

    // Returns true when this pushed burnout to the max, so the caller stops its flow.
    private bool ChangeBurnout(float amount)
    {
        if (amount >= 0f) outlet.burnoutMeter.IncreaseBurnout(amount);
        else outlet.burnoutMeter.DecreaseBurnout(-amount);

        if (outlet.burnoutMeter.Burnout < BurnoutMeter.Max) return false;

        GameOver();
        return true;
    }

    private void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        afterBreak = null;
        disposeBag.Dispose();
        GameModeInteractable.Allow(InteractionGroup.None);
        outlet.eyelids.Close(3);
        MonologueService.Interrupt(MonologueIds.Monologue_Demo, ResetScene); // TODO: game over monologue
    }

    private static void SetTime(string time)
    {
        UEventBus<TimeChangedEvent>.Raise(new TimeChangedEvent(time));
    }

    private void ResetScene()
    {
        UNavigationController.SetRootViewController(new GameplayController());
    }

    #endregion

    #region Choices
    
    private void ShowChoice01()
    {
        outlet.eyelids.Close(2);
        ShowMultipleChoiceView(
            ("Nah, I'll keep working, I gotta finish this all today!", () => KeepWorking(ShowSlackCall)),
            ("Break, please!!!", () => TakeBreak(ShowSlackCall)));
    }

    private void ShowSlackCall()
    {
        outlet.desktopUI.ShowSlackCall(); // also starts the slack ringtone
        MonologueService.Play(MonologueIds.RightTheMeeting); // TODO: slack call monologue
    }

    // DesktopUI already stopped the ringtone and hid the call.
    private void OnSlackCallAnswered(SlackCallAnsweredEvent data)
    {
        SetTime("13:22");
        if (ChangeBurnout(TaskBurnout)) return;
        GameModeInteractable.Allow(InteractionGroup.None);
        outlet.eyelids.Close(1);
        DelayedExecutionManager.ExecuteActionAfterDelay(3000, AfterSlackCall).disposeBy(disposeBag);
    }

    private void AfterSlackCall()
    {
        outlet.eyelids.Open(2);
        MonologueService.Play(MonologueIds.ThisMeetingWasExausting, ShowChoice02);
    }

    private void ShowChoice02()
    {
        outlet.eyelids.Close(2);
        ShowMultipleChoiceView(
            ("Keep grinding. Live, laugh, work", () => KeepWorking(StartLogoFeedbackTask)),
            ("Need. A. Break.", () => TakeBreak(StartLogoFeedbackTask)));
    }

    // Pushing through costs burnout; the next task starts 3s later.
    private void KeepWorking(Action nextTask)
    {
        if (ChangeBurnout(KeepWorkingBurnout)) return;
        OpenEyesAfterChoice();
        ResumeWork(nextTask);
    }

    // The next task waits until the player does something relaxing (OnBreakActionDone).
    private void TakeBreak(Action nextTask)
    {
        afterBreak = () => ResumeWork(nextTask);
        OpenEyesAfterChoice();
        StartBreak();
    }

    // If the player does nothing for a while, an idle monologue plays once and relieves some burnout.
    private void StartBreak()
    {
        SetMode(GameMode.Break);
        StopIdleTimer();
        idleTicket = DelayedExecutionManager.ExecuteActionAfterDelay(IdleDelay, PlayIdleMonologue);
        idleTicket.disposeBy(disposeBag);
    }

    private void StopIdleTimer()
    {
        if (idleTicket != null) DelayedExecutionManager.CancelTicket(idleTicket);
        idleTicket = null;
    }

    private void PlayIdleMonologue()
    {
        idleTicket = null;
        ChangeBurnout(-IdleBurnoutRelief);
        MonologueService.Play(MonologueIds.Idle);
    }

    private void ResumeWork(Action nextTask)
    {
        StopIdleTimer();
        afterBreak = null;
        SetMode(GameMode.Task);
        DelayedExecutionManager.ExecuteActionAfterDelay(3000, nextTask).disposeBy(disposeBag);
    }

    private void OnBreakActionDone()
    {
        if (ChangeBurnout(-BreakBurnoutRelief)) return;

        var next = afterBreak;
        afterBreak = null;
        next?.Invoke();
    }

    private void OpenEyesAfterChoice()
    {
        outlet.eyelids.Open(2);
        DelayedExecutionManager.ExecuteActionAfterDelay(3500, () => outlet.eyelids.Blink()).disposeBy(disposeBag);
    }

    private void ShowMultipleChoiceView(params (string text, Action onPicked)[] choices)
    {
        if (multipleChoiceView != null && multipleChoiceView.IsActive)
        {
            multipleChoiceView.RemoveView(0f, null, AnimationType.ScaleDownFromMiddle);
            return;
        }
        multipleChoiceView = new MultipleChoiceView(outlet.canvas.transform, uiStackController, choices);
        multipleChoiceView.PresentView(2f, AnimationType.ScaleUpFromMiddle);
    }
    #endregion

    #region Items
    
    private void OnItemClicked(ItemClickedEvent data)
    {
        StopIdleTimer();
        var item = data.source.GetComponent<ClickableItem>();
        if (item == outlet.window)
        {
            OnWindowClicked();
            return;
        }

        switch (data.id)
        {
            case "Item_Maca":
                PlayCatView(item); break;
            case "Item_Polaroid_Friends":
                PlaySlika1View(item); break;
            // case "Monitor":; break;
        }
    }
    
    private void PlayCatView(ClickableItem item)
    {
        Debug.Log("Playing Cat");
        item.Clickable = false;
        var clip = Resources.Load<VideoClip>(CatVideoPath);
        new VideoView(clip, outlet.canvas.transform, uiStackController, OnBreakActionDone).PresentView(0.3f, AnimationType.ScaleUpFromMiddle);
    }
    
    private void PlaySlika1View(ClickableItem item)
    {
        Debug.Log("Playing Item_Polaroid_Friends");
        coloringItem = item;
        ShowImageColoringView(GetSpriteForImage("Item_Polaroid_Friends"));
    }

    private void OnImageColoringDone(ImageColoringDoneEvent data)
    {
        if (coloringItem != null) coloringItem.Clickable = false;
        coloringItem = null;
        OnBreakActionDone();
    }

    #endregion
    
    #region Other

    
    private void OnGameOver(GameOverEvent args)
    {
        GameOver();
    }

    private void OnModeChanged(ModeChangedEvent mode)
    {
        // zoom, parallax and what is clickable are handled by GameModeHandler / GameModeInteractable;
        GameModeHandler.Set(mode.mode);
        ToggleClickableElements(mode.mode);
    }

    // ToggleClickableElements too, since GameModeHandler skips the event when the mode doesn't change.
    private void SetMode(GameMode mode)
    {
        GameModeHandler.Set(mode);
        ToggleClickableElements(mode);
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

    private void StartGame()
    {
        // turn screen on
        PlayDesktopUnlock();
        DelayedExecutionManager.ExecuteActionAfterDelay(1500, StartInitialMonologue).disposeBy(disposeBag);
        // hide buttons
        // enable input
    }
    
    private void PlayDesktopUnlock()
    {
        outlet.desktopUnlock.PlayState(FlexibleState.Highlighted);
        outlet.desktopUnlock.raycastTarget = false;
        outlet.playButton.interactable = false;
        outlet.playButton.gameObject.SetActive(false);
        outlet.desktopUI.HideLockedScreen();
        GameModeInteractable.Allow(InteractionGroup.Desktop);
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
    #endregion
    public override void SceneWillDisappear()
    {
        base.SceneWillDisappear();
        outlet.playButton.Unregister(StartGame);
        MonologueService.Stop(true);
        MonologueService.ClearHistory();
        UEventBus<ItemClickedEvent>.Deregister(itemClickedBinding);
        UEventBus<ModeChangedEvent>.Deregister(modeChangedEvent);
        UEventBus<GameOverEvent>.Deregister(gameOverBinding);
        UEventBus<MailTaskDoneEvent>.Deregister(mailTaskDoneBinding);
        UEventBus<ImageColoringDoneEvent>.Deregister(imageColoringDoneBinding);
        UEventBus<SlackCallAnsweredEvent>.Deregister(slackCallAnsweredBinding);
        UEventBus<LogoTaskDoneEvent>.Deregister(logoTaskDoneBinding);
        GameTicker.SharedInstance.Update -= DebugModeKeys;
        disposeBag.Dispose();
    }
}
