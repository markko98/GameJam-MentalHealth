using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Exactly one tab is on at a time; each tab can show its own page.
[RequireComponent(typeof(ToggleGroup))]
public class UITabGroup : MonoBehaviour
{
    [Serializable]
    private struct Tab
    {
        public UIToggle toggle;
        public GameObject page;
    }

    [Tooltip("Leave empty to use the UIToggles that are direct children, in hierarchy order.")]
    [SerializeField] private List<Tab> tabs = new List<Tab>();
    [SerializeField, Min(0)] private int defaultTab;

    private readonly UIListeners<int> listeners = new UIListeners<int>();
    private ToggleGroup group;
    private bool isInitialized;

    public int SelectedIndex { get; private set; } = -1;
    public int Count => tabs.Count;

    public UIToggle GetToggle(int index) => tabs[index].toggle;

    public void Register(Action<int> listener, Object owner = null) => listeners.Add(listener, listener, owner);
    public void Unregister(Action<int> listener) => listeners.Remove(listener);
    public void UnregisterAll() => listeners.Clear();

    public int Add(UIToggle toggle, GameObject page = null)
    {
        Initialize();

        var existing = tabs.FindIndex(tab => tab.toggle == toggle);
        if (existing >= 0) return existing;

        tabs.Add(new Tab { toggle = toggle, page = page });
        Bind(tabs.Count - 1);
        if (page != null) page.SetActive(false);
        return tabs.Count - 1;
    }

    public void Select(int index)
    {
        Initialize();
        if (index < 0 || index >= tabs.Count || index == SelectedIndex) return;

        SelectedIndex = index;
        for (var i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].toggle != null) tabs[i].toggle.SetIsOnWithoutNotify(i == index);
            if (tabs[i].page != null) tabs[i].page.SetActive(i == index);
        }

        listeners.Invoke(index);
    }

    // Next (+1) or previous (-1) tab, skipping disabled ones; stops at the ends.
    public bool SelectAdjacent(int step)
    {
        var direction = Math.Sign(step);
        if (direction == 0) return false;

        for (var i = SelectedIndex + direction; i >= 0 && i < tabs.Count; i += direction)
        {
            var toggle = tabs[i].toggle;
            if (toggle == null || !toggle.IsInteractable()) continue;

            Select(i);
            return true;
        }

        return false;
    }

    public void SetTabInteractable(int index, bool interactable)
    {
        if (index >= 0 && index < tabs.Count && tabs[index].toggle != null) tabs[index].toggle.interactable = interactable;
    }

    private void Awake() => Initialize();

    private void Start()
    {
        if (SelectedIndex < 0 && tabs.Count > 0) Select(Mathf.Min(defaultTab, tabs.Count - 1));
    }

    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        group = GetComponent<ToggleGroup>();
        group.allowSwitchOff = false;

        if (tabs.Count == 0)
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<UIToggle>(out var toggle)) tabs.Add(new Tab { toggle = toggle });
            }
        }

        for (var i = 0; i < tabs.Count; i++) Bind(i);
    }

    private void Bind(int index)
    {
        var toggle = tabs[index].toggle;
        if (toggle == null) return;

        toggle.group = group;
        toggle.Register(isOn =>
        {
            if (isOn) Select(index);
        }, this);
    }
}
