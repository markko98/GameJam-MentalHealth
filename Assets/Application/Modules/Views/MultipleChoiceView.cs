using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class MultipleChoiceView : UIViewController
{
    private const string ResourcePath = "MultipleChoiceView";

    private readonly (string text, Action onPicked)[] choices;
    private MultipleChoiceViewOutlet outlet;
    private bool picked;

    public MultipleChoiceView(Transform viewport, UIStackNavigationController controller, params (string text, Action onPicked)[] choices) : base(controller)
    {
        this.choices = choices;
        view = Object.Instantiate(Resources.Load<GameObject>(ResourcePath), viewport, false);
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        outlet = view.GetComponentInChildren<MultipleChoiceViewOutlet>(true);

        foreach (var (text, onPicked) in choices)
        {
            var button = Object.Instantiate(outlet.choiceButtonPrefab, outlet.choicesParent, false).GetComponentInChildren<UIButton>();
            button.Text = text;
            button.Register(() => Pick(onPicked));
        }
    }

    public override void ViewDidDisappear()
    {
        base.ViewDidDisappear();
        Object.Destroy(view);
    }

    private void Pick(Action onPicked)
    {
        if (picked) return;

        picked = true;
        RemoveView();
        onPicked?.Invoke();
    }
}
