using UnityEngine;

public enum GameMode
{
    Break, Task, Other
}
public class GameplayController : USceneController
{
    private GameplayOutlet outlet;
    private EventBinding<ItemClickedEvent> itemClickedBinding;
    private EventBinding<ModeChangedEvent> modeChangedEvent;

    public GameplayController() : base(SceneNames.Gameplay)
    {
    }

    public override void SceneDidLoad()
    {
        base.SceneDidLoad();
        outlet = GameObject.Find(OutletNames.Gameplay).GetComponent<GameplayOutlet>();
        modeChangedEvent = new EventBinding<ModeChangedEvent>(OnModeChanged);
        UEventBus<ModeChangedEvent>.Register(modeChangedEvent);
        itemClickedBinding = new EventBinding<ItemClickedEvent>(OnItemClicked);
        UEventBus<ItemClickedEvent>.Register(itemClickedBinding);
        StartDemoMonologue();
    }

    private void OnModeChanged(ModeChangedEvent mode)
    {
        // depending on mode enable/disable items
        // zoom camera in to desktop on task mode
        // zoom out camera out on break mode
    }

    private void StartDemoMonologue()
    {
        MonologueService.Play(MonologueIds.Dialogue_Intro);
    }

    public override void SceneWillAppear()
    {
        base.SceneWillAppear();
    }

    public override void SceneWillDisappear()
    {
        base.SceneWillDisappear();
        outlet.playButton.Register(StartGame);
        MonologueService.Stop(true);
        MonologueService.ClearHistory();
        UEventBus<ItemClickedEvent>.Deregister(itemClickedBinding);
    }

    private void StartGame()
    {
        // TODO: turn screen on
        // hide buttons
        // enable input
    }
    
    private void OnItemClicked(ItemClickedEvent data)
    {
        switch (data.id)
        {
            case "Item_Maca":
                PlayCatView(); break;
            case "Item_Slika1":
                PlayCatView(); break;
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
        // show black screen and play purring sound
        Debug.Log("Playing Slika1");
    }
}
