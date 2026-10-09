using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using TMPro;
using Object = UnityEngine.Object;
using Sequence = DG.Tweening.Sequence;

public class UIViewController
{
    protected GameObject _view;

    public GameObject view
    {
        get { return _view; }
        set
        {
            _view = value;
            _view.SetActive(false);
            InitializeUIElements();
        }
    }

    private bool isActive;
    public bool IsActive => isActive && view != null && view.activeInHierarchy;

    public List<Button> buttons = new List<Button>();
    public List<Toggle> toggles = new List<Toggle>();
    public List<Slider> sliders = new List<Slider>();
    public Action[] blocks;

    // public Text[] texts; // HANDLE TMPRo
    public TextMeshProUGUI[] texts;
    public UIViewComponent[] subviews;

    public UIImageViewComponent[] imageSubviews;

    protected UIStackNavigationController navigationController;

    List<ButtonHandler> buttonHandlers;
    List<ToggleHandler> toggleHandlers;
    List<SliderHandler> sliderHandlers;

    // background blur image 
    private Image backgroundBlurImage;
    private Object backgroundBlurImageObj;
    private Sequence backgroundBlurImageSequence;

    public UIViewController(GameObject view, UIStackNavigationController controller)
    {
        this.view = view;
        navigationController = controller;
    }

    public UIViewController(UIStackNavigationController controller)
    {
        navigationController = controller;
    }

    public virtual void ViewDidLoad()
    {
    }

    public virtual void ViewWillAppear()
    {
        isActive = true;
    }

    public virtual void ViewDidAppear()
    {
        isActive = true;
        foreach (var button in buttons)
        {
            button.interactable = true;
        }
    }

    private void DissmissAction()
    {
        navigationController.PopController(0);
    }

    public virtual void ViewDidDisappear()
    {
        isActive = false;
    }

    public virtual void ViewWillDisappear()
    {
        isActive = false;
        foreach (var button in buttons)
        {
            button.interactable = false;
        }
    }

    public virtual void PresentView(float delay = 0,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        navigationController.Push(this, delay, animationType);
    }

    public virtual void RemoveView(float delay = 0, Action dismissCallback = null,
        AnimationType animationType = AnimationType.ScaleDownFromMiddle)
    {
        navigationController.PopController(delay, animationType);
        dismissCallback?.Invoke();
    }

    public virtual void PresentViewAsPopup(bool showBackgroundBlurImage = true, float delay = 0,
        AnimationType animationType = AnimationType.ScaleUpFromMiddle)
    {
        //TODO: make the pop up jump from its parent's position
        navigationController.PresentPopup(this, delay, animationType);
        if (showBackgroundBlurImage)
        {
            ShowBackgroundBlurImage();
        }
    }

    public virtual void RemovePopUp(float delay = 0, Action onComplete = null,
        AnimationType animationType = AnimationType.ScaleDownFromMiddle)
    {
        navigationController.RemovePopup(this, delay, onComplete, animationType);
        HideBackgroundBlurImage();
    }

    protected void InitializeUIElements()
    {
        var buttonArray = view.GetComponentsInChildren<Button>();

        foreach (var button in buttonArray)
        {
            buttons.Add(button);
        }

        buttonHandlers = new List<ButtonHandler>();

        foreach (var button in buttons)
        {
            buttonHandlers.Add(new ButtonHandler(button,
                () => { Debug.Log("Button named :" + button.name + " index:" + buttons.IndexOf(button)); }));
        }

        foreach (var handler in buttonHandlers)
        {
            handler.button.onClick.AddListener(() =>
            {
                handler.block();
                //TODO: setup haptic
                // HapticManager.PlayIntervalVibration(VibrationInterval.ButtonClicked);
            });
        }

        var toggleArray = view.GetComponentsInChildren<Toggle>(true);

        foreach (var toggle in toggleArray)
        {
            toggles.Add(toggle);
        }

        toggleHandlers = new List<ToggleHandler>();

        foreach (var toggle in toggles)
        {
            toggleHandlers.Add(new ToggleHandler(toggle,
                isOn => Debug.Log("Toggle named :" + toggle.name + " isOn:" + isOn)));
        }

        foreach (var handler in toggleHandlers)
        {
            handler.toggle.onValueChanged.AddListener(isOn => handler.block(isOn));
        }

        var sliderArray = view.GetComponentsInChildren<Slider>(true);

        foreach (var slider in sliderArray)
        {
            sliders.Add(slider);
        }

        sliderHandlers = new List<SliderHandler>();

        foreach (var slider in sliders)
        {
            sliderHandlers.Add(new SliderHandler(slider,
                value => Debug.Log("Slider named :" + slider.name + " value:" + value)));
        }

        foreach (var handler in sliderHandlers)
        {
            handler.slider.onValueChanged.AddListener(value => handler.block(value));
        }

        texts = view.GetComponentsInChildren<TextMeshProUGUI>(true);

        subviews = view.GetComponentsInChildren<UIViewComponent>(true);

        imageSubviews = view.GetComponentsInChildren<UIImageViewComponent>(true);

        //TODO: setup auto blur
        // backgroundBlurImageObj = Resources.Load(Strings.UISharedResourcesPath + "BackgroundBlurImage");

        //call view did load on derived class
        ViewDidLoad();
    }

    public void ShowBackgroundBlurImage()
    {
        return;

        backgroundBlurImageSequence?.Kill(true);
        // backgroundBlurImage = Object.Instantiate(backgroundBlurImageObj, PersistentReferences.Instance.fullScreenViewport).GetComponent<Image>(); // TODO:
        backgroundBlurImage.transform.SetAsFirstSibling();
        backgroundBlurImageSequence = DOTween.Sequence();
        backgroundBlurImageSequence.Append(backgroundBlurImage.DOFade(0, 0));
        backgroundBlurImageSequence.Append(backgroundBlurImage.DOFade(0.9f, 0.2f));
    }

    public void HideBackgroundBlurImage()
    {
        return;
        if (backgroundBlurImage == null) return;

        backgroundBlurImageSequence?.Kill();
        backgroundBlurImageSequence = DOTween.Sequence();
        backgroundBlurImageSequence.Append(backgroundBlurImage.DOFade(0, 0.2f));
        backgroundBlurImageSequence.OnComplete(() => { Object.Destroy(backgroundBlurImage.gameObject); });
    }

    public virtual void RegisterPressedButtonWithIndex(int index, Action block)
    {
        ButtonHandler handler = buttonHandlers[index];
        handler.block = block;
    }

    public virtual Button GetButtonWithName(string name)
    {
        bool didFindButton = false;
        foreach (var handler in buttonHandlers)
        {
            if (handler.button.name == name)
            {
                return handler.button;
            }
        }

        if (!didFindButton)
        {
            Debug.LogWarning("Couldnt find button with name :" + name);
        }

        return null;
    }

    public virtual void RegisterPressedButtonWithName(string name, Action block)
    {
        bool didFindButton = false;
        foreach (var handler in buttonHandlers)
        {
            if (handler.button.name == name)
            {
                handler.block = block;
                didFindButton = true;
            }
        }

        if (!didFindButton)
        {
            Debug.LogWarning("Couldnt find button with name :" + name);
        }
    }

    public virtual void RegisterToggleWithName(string name, Action<bool> block)
    {
        bool didFindToggle = false;
        foreach (var handler in toggleHandlers)
        {
            if (handler.toggle.name == name)
            {
                handler.block = block;
                didFindToggle = true;
            }
        }

        if (!didFindToggle)
        {
            Debug.LogWarning("Couldnt find toggle with name :" + name);
        }
    }

    public virtual void RegisterSliderWithName(string name, Action<float> block)
    {
        bool didFindSlider = false;
        foreach (var handler in sliderHandlers)
        {
            if (handler.slider.name == name)
            {
                handler.block = block;
                didFindSlider = true;
            }
        }

        if (!didFindSlider)
        {
            Debug.LogWarning("Couldnt find slider with name :" + name);
        }
    }

    public Toggle GetToggleWithName(string name)
    {
        foreach (var toggle in toggles)
        {
            if (toggle.name == name)
            {
                return toggle;
            }
        }

        Debug.LogError("Coudnt find Toggle named:" + name);
        return null;
    }

    public Slider GetSliderWithName(string name)
    {
        foreach (var slider in sliders)
        {
            if (slider.name == name)
            {
                return slider;
            }
        }

        Debug.LogError("Coudnt find Slider named:" + name);
        return null;
    }

    public TextMeshProUGUI GetTextWithName(string textName)
    {
        foreach (var text in texts)
        {
            if (text.name == textName)
            {
                return text;
            }
        }

        Debug.LogError("Coudnt find Text named:" + textName);
        return null;
    }

    public UIViewComponent GetUIViewComponentWithName(string name)
    {
        foreach (var uiview in subviews)
        {
            if (uiview.name == name)
            {
                return uiview;
            }
        }

        Debug.LogError("Coudnt find UIViewComponent named:" + name);
        return null;
    }

    public UIImageViewComponent GetUIImageViewComponentWithName(string name)
    {
        foreach (var uiview in imageSubviews)
        {
            if (uiview.name == name)
            {
                return uiview;
            }
        }

        Debug.LogError("Coudnt find UIImageViewComponent named:" + name);
        return null;
    }


    public virtual void InitializeWithView(GameObject view)
    {
        this.view = view;
    }

    public class ButtonHandler
    {
        public Button button;
        public Action block;

        public ButtonHandler(Button button, Action block)
        {
            this.button = button;
            this.block = block;
        }
    }

    public class ToggleHandler
    {
        public Toggle toggle;
        public Action<bool> block;

        public ToggleHandler(Toggle toggle, Action<bool> block)
        {
            this.toggle = toggle;
            this.block = block;
        }
    }

    public class SliderHandler
    {
        public Slider slider;
        public Action<float> block;

        public SliderHandler(Slider slider, Action<float> block)
        {
            this.slider = slider;
            this.block = block;
        }
    }
}