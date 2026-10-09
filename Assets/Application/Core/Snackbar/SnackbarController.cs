using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

// Generic toast/snackbar queue. Requires a prefab at Resources/Snackbar with a
// SnackbarOutlet component wired up (frameRef, contentHolder, backgroundImage,
// dismissButton, titleText) — build that once in the Editor, this script can't
// author a UI prefab itself.
public class SnackbarController
{
    private static SnackbarController _instance;

    public static SnackbarController Instance
    {
        get
        {
            if (_instance == null) Initialize();
            return _instance;
        }
    }

    private const string ResourcesPath = "Snackbar";

    private readonly SnackbarOutlet outlet;
    private readonly Queue<ISnackbarContent> snackbarQueue;

    private static GameObject snackbarInstance;
    private static bool snackbarShown;
    private static Transform currentParentCanvas;

    public Action SnackbarClosed;
    private float snackbarHideDelay = 2.5f;

    private SnackbarController()
    {
        var snackbarPrefab = Resources.Load<GameObject>(ResourcesPath);
        snackbarInstance = Object.Instantiate(snackbarPrefab);

        outlet = snackbarInstance.GetComponent<SnackbarOutlet>();
        snackbarShown = false;
        snackbarInstance.SetActive(snackbarShown);

        snackbarQueue = new Queue<ISnackbarContent>();

        outlet.dismissButton.onClick.AddListener(() => StartCloseAnimation());
    }

    public static void Initialize(Transform parentCanvas = null)
    {
        if (_instance != null) return;

        _instance = new SnackbarController();
        SetParentCanvas(parentCanvas);

        GameObject.DontDestroyOnLoad(snackbarInstance);
    }

    public static void SetParentCanvas(Transform parentCanvas)
    {
        currentParentCanvas = parentCanvas;
    }

    public void QueueSnackbar(ISnackbarContent content)
    {
        snackbarQueue.Enqueue(content);

        if (!snackbarShown)
        {
            ShowNextSnackbar();
        }
    }

    private void ShowNextSnackbar()
    {
        if (snackbarQueue.Count == 0) return;

        var nextContent = snackbarQueue.Dequeue();
        ShowSnackbar(nextContent);
    }

    private void ShowSnackbar(ISnackbarContent content)
    {
        if (snackbarShown) return;

        if (currentParentCanvas == null)
        {
            Debug.LogWarning("SnackbarController: set a parent canvas via SetParentCanvas before showing a snackbar.");
            return;
        }

        snackbarInstance.transform.SetParent(currentParentCanvas, false);
        snackbarShown = true;

        snackbarInstance.SetActive(true);
        content.GetContent().transform.SetParent(outlet.contentHolder, false);
        content.Initialize();
        content.SetTitle(outlet.titleText);
        StartSlideAnimation();

        content.SnackbarContentDisplayFinished += () => StartCloseAnimation(true);
    }

    private Tween StartSlideAnimation()
    {
        var height = outlet.frameRef.rect.height;
        var s = DOTween.Sequence();
        s.Append(outlet.backgroundImage.DOFade(snackbarShown ? 0.7f : 0, 0.3f));
        s.Append(outlet.frameRef.transform.DOMoveY(snackbarShown ? 0 : -height, 0.3f));
        return s;
    }

    private void StartCloseAnimation(bool hasDelay = false)
    {
        if (!snackbarShown) return;

        snackbarShown = false;

        DOTween.Sequence().AppendInterval(hasDelay ? snackbarHideDelay : 0)
            .Append(StartSlideAnimation())
            .OnComplete(CleanUp);
    }

    private void CleanUp()
    {
        snackbarInstance.transform.SetParent(null, false);
        snackbarInstance.SetActive(false);

        foreach (Transform child in outlet.contentHolder.transform)
        {
            Object.Destroy(child.gameObject);
        }

        Object.DontDestroyOnLoad(snackbarInstance);
        SnackbarClosed?.Invoke();
        ShowNextSnackbar();
    }
}
