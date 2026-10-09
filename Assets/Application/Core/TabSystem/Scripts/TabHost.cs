using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// Code-driven tabs for a screen: spawns a tab button per page into a UITabGroup bar and
// switches TabBaseView pages through UIPagesViewController (slide transitions).
//
//   tabs = new TabHost<MenuTab>(outlet.tabBar, outlet.tabPrefab, navigationController)
//       .Add(MenuTab.Journal, "Dnevnik", new JournalTab(...))
//       .Add(MenuTab.Settings, "Postavke", new SettingsTab(...));
//   tabs.Build(MenuTab.Journal);
public sealed class TabHost<TId>
{
    private readonly UITabGroup group;
    private readonly UIToggle tabPrefab;
    private readonly UIPagesViewController pages;
    private readonly List<TId> ids = new List<TId>();
    private readonly List<TabBaseView> views = new List<TabBaseView>();
    private readonly UIListeners<TId> listeners = new UIListeners<TId>();

    private int current = -1;
    private bool isBuilt;
    private SwipeGesture swipe;

    public TabHost(RectTransform tabBar, UIToggle tabPrefab, UIStackNavigationController navigation)
    {
        if (!tabBar.TryGetComponent(out group)) group = tabBar.gameObject.AddComponent<UITabGroup>();
        this.tabPrefab = tabPrefab;
        pages = new UIPagesViewController(navigation);
        group.Register(OnGroupSelected);
    }

    public TId Selected => current >= 0 ? ids[current] : default;
    public TabBaseView SelectedView => current >= 0 ? views[current] : null;

    public TabHost<TId> Add(TId id, string title, TabBaseView view)
    {
        if (isBuilt) throw new InvalidOperationException("Add every tab before Build().");

        var toggle = Object.Instantiate(tabPrefab, group.transform);
        toggle.name = $"Tab_{id}";
        toggle.Text = title;
        group.Add(toggle);

        ids.Add(id);
        views.Add(view);
        return this;
    }

    public void Build(TId initial)
    {
        var start = Mathf.Max(0, IndexOf(initial));
        isBuilt = true;
        pages.SetPages(new List<UIViewController>(views), start);
        group.Select(start);

        // The group may already have selected this tab on its own (Start) before Build, so no event came.
        if (current < 0) OnGroupSelected(group.SelectedIndex);
    }

    public void Select(TId id) => group.Select(IndexOf(id));

    // Next (+1) or previous (-1) enabled tab; returns whether the tab changed.
    public bool SelectAdjacent(int step) => group.SelectAdjacent(step);

    public void SetEnabled(TId id, bool isEnabled) => group.SetTabInteractable(IndexOf(id), isEnabled);

    public void Register(Action<TId> listener, Object owner = null) => listeners.Add(listener, listener, owner);
    public void Unregister(Action<TId> listener) => listeners.Remove(listener);

    // Flick left/right anywhere on swipeArea to change tab. canSwipe can veto it (the active tab's
    // BlocksTabSwipe is always checked). catchGaps lets swipes start on empty background too.
    public void EnableSwipe(GameObject swipeArea, Func<bool> canSwipe = null, bool catchGaps = true)
    {
        DisableSwipe();
        swipe = SwipeGesture.Attach(swipeArea, catchGaps: catchGaps);
        if (swipe == null) return;

        swipe.CanStart = () => SelectedView?.BlocksTabSwipe != true && (canSwipe == null || canSwipe());
        swipe.OnSwipe += OnSwipe;
    }

    public void DisableSwipe()
    {
        if (swipe == null) return;
        swipe.OnSwipe -= OnSwipe;
        swipe.CanStart = null;
        swipe = null;
    }

    public void Cleanup()
    {
        DisableSwipe();
        group.Unregister(OnGroupSelected);
        listeners.Clear();
        foreach (var view in views) view.Cleanup();
        views.Clear();
        ids.Clear();
        current = -1;
    }

    private void OnGroupSelected(int index)
    {
        if (!isBuilt || index == current || index < 0 || index >= views.Count) return;

        if (current >= 0) views[current].OnTabDeselected();
        pages.GoToPage(index);
        current = index;
        views[index].OnTabSelected();
        listeners.Invoke(ids[index]);
    }

    // The finger travelling left drags the next tab in from the right.
    private void OnSwipe(Orientation direction)
    {
        if (direction == Orientation.Left) SelectAdjacent(1);
        else if (direction == Orientation.Right) SelectAdjacent(-1);
    }

    private int IndexOf(TId id) => ids.FindIndex(x => EqualityComparer<TId>.Default.Equals(x, id));
}
