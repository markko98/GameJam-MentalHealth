using System.Threading.Tasks;
using UnityEngine;

// Drives Demo/UIShowcase.unity and doubles as a reference for the UI components' code API.
//
// Screen-level tabs built from code (pages are TabBaseView view controllers):
//
//   var tabs = new TabHost<MenuTab>(outlet.tabBar, outlet.tabPrefab, navigationController)
//       .Add(MenuTab.Breathe, "Disanje", new BreatheTab(navigationController))
//       .Add(MenuTab.Journal, "Dnevnik", new JournalTab(navigationController));
//   tabs.Build(MenuTab.Breathe);
//   tabs.Register(tab => Debug.Log($"Now on {tab}"), this);
//   tabs.EnableSwipe(outlet.gameObject);
//   tabs.SetEnabled(MenuTab.Journal, false);
//   ...
//   tabs.Cleanup();
//
// Monologues are MonologueDefinition assets played by id: MonologueService.Play(MonologueIds.Intro).
public class UIShowcase : MonoBehaviour
{
    private static readonly string[] TabPages =
    {
        "Udahni polako kroz nos četiri sekunde, zadrži dah, pa izdahni kroz usta.",
        "Zapiši jednu stvar koja te danas razveselila, koliko god bila mala.",
        "Ovdje bi bile postavke. Isključi toggle \"Gumbi\" da vidiš onemogućene gumbe."
    };

    private const string Story =
        "Typewriter ispisuje tekst slovo po slovo, s kratkim pauzama na interpunkciji. " +
        "Radi i s <b>rich text</b> <color=#F89B17>tagovima</color>.";

    [Header("Buttons")]
    [SerializeField] private UIButton clickButton;
    [SerializeField] private UIButton asyncButton;
    [SerializeField] private UIButton holdButton;
    [SerializeField] private UILabel buttonStatus;
    [SerializeField] private UILabel clickCounter;

    [Header("Text")]
    [SerializeField] private UILabel story;
    [SerializeField] private UIButton typeButton;
    [SerializeField] private UIButton fadeButton;
    [SerializeField] private UISubtitles subtitles;
    [SerializeField] private MonologueDefinition demoMonologue;
    [SerializeField] private UIButton subtitlesButton;
    [SerializeField] private UIButton nextButton;

    [Header("Toggles & tabs")]
    [SerializeField] private UIToggle soundToggle;
    [SerializeField] private UIToggle buttonsToggle;
    [SerializeField] private UITabGroup tabs;
    [SerializeField] private UILabel tabPage;

    private int clicks;
    private bool isStoryFaded;

    private void Start()
    {
        // Buttons: Register takes any method or lambda; listeners owned by a destroyed object drop out by themselves.
        clickButton.Register(OnClicked);
        asyncButton.RegisterAsync(SaveAsync);
        holdButton.Register(() => SetStatus("Potvrđeno držanjem!"), this);
        clickCounter.SetNumber(clicks);

        // Text
        typeButton.Register(TypeStory);
        fadeButton.Register(ToggleStoryFade);
        subtitlesButton.Register(PlayMonologue);
        nextButton.Register(Next);
        TypeStory();

        // Toggles: SetIsOnWithoutNotify sets the starting state without firing listeners.
        soundToggle.SetIsOnWithoutNotify(!AudioService.SoundMuted);
        soundToggle.Register(isOn => AudioService.SetSoundMuted(!isOn), this);
        buttonsToggle.SetIsOnWithoutNotify(true);
        buttonsToggle.Register(SetButtonsEnabled);

        // Tabs: the group picks its default tab on Start, so also show whatever is selected already.
        tabs.Register(ShowTab);
        ShowTab(Mathf.Max(0, tabs.SelectedIndex));
    }

    private void OnClicked()
    {
        clicks++;
        clickCounter.SetNumber(clicks);
        SetStatus($"Klik #{clicks}");
    }

    // The button stays locked (and greyed out) until this task finishes.
    private async Task SaveAsync()
    {
        SetStatus("Spremam...");
        await Task.Delay(1500);
        if (this != null) SetStatus("Spremljeno!");
    }

    private void TypeStory()
    {
        isStoryFaded = false;
        story.Alpha = 1f;
        story.Type(Story, () => SetStatus("Tekst ispisan."));
    }

    private void ToggleStoryFade()
    {
        isStoryFaded = !isStoryFaded;
        story.FadeTo(isStoryFaded ? 0.15f : 1f, 0.4f);
    }

    private void PlayMonologue()
    {
        if (demoMonologue != null)
        {
            MonologueService.Play(demoMonologue, () => SetStatus("Monolog gotov."));
            return;
        }

        subtitles.Show("Dodaj MonologueDefinition u polje \"Demo Monologue\" da vidiš monolog.");
        SetStatus("Nema monologa - Tools > Create > Monologue System.");
    }

    private void Next()
    {
        if (MonologueService.IsPlaying) MonologueService.Next();
        else subtitles.Next();
    }

    private void SetButtonsEnabled(bool isEnabled)
    {
        clickButton.interactable = isEnabled;
        asyncButton.interactable = isEnabled;
        holdButton.interactable = isEnabled;
    }

    private void ShowTab(int index)
    {
        if (index < TabPages.Length) tabPage.Type(TabPages[index]);
    }

    private void SetStatus(string text)
    {
        buttonStatus.Text = text;
        buttonStatus.Color = ColorProvider.Colors.highlight;
    }
}
