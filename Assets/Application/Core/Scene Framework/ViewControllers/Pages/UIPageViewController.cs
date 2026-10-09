#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public interface IUIPageViewControllerResponder
{
    void PageControllerSelectedPage(UIPageViewController pageController, int index, GameObject gameObject);
    void PageControllerScrollingOverPage(UIPageViewController pageController, int index, GameObject gameObject);
    void PageControllerDeselectedPage(UIPageViewController pageController, int index, GameObject gameObject);
}

public class UIPageViewController : MonoBehaviour
{
    enum UIPageViewControllerOrientation
    {
        Vertical, Horizontal
    }

    private List<GameObject> pages = new List<GameObject>();
    private List<GameObject> prefixPages = new List<GameObject>();
    private List<GameObject> sufixPages = new List<GameObject>();

    private float margin = 5f;

    private float[] pagesPositions;

    public GameObject contentPanel;

    private bool dragging = false;
    private bool shouldSnap = false;
    private int targetPageIndex;

    private int _selectedIndex;
    public int selectedIndex {
        get => _selectedIndex;
        private set
        {
            if (_selectedIndex == value) return;
            OnDeselectedPage?.Invoke(_selectedIndex, pages[_selectedIndex]);
            responderDelegate?.PageControllerDeselectedPage(this, _selectedIndex, gameObject: pages[_selectedIndex]);

            _selectedIndex = value;

            OnSelectedPage?.Invoke(_selectedIndex, pages[_selectedIndex]);
            responderDelegate?.PageControllerSelectedPage(this, _selectedIndex, gameObject: pages[_selectedIndex]);
        }
    }

    private UIPageViewControllerOrientation orientation = UIPageViewControllerOrientation.Vertical;

    public IUIPageViewControllerResponder? responderDelegate;
    public Action<int, GameObject>? OnSelectedPage;
    public Action<int, GameObject>? OnDeselectedPage;
    public Action<int, GameObject>? OnScrollingOverPage;

    [SerializeField] private ScrollRect scrollRect;

    void Update()
    {
        if (pages.Count < 1)
        {
            return;
        }
        if (dragging)
        {
            return;
        }

        if (shouldSnap)
        {
            LerpToElement(targetPageIndex);
        }
        shouldSnap = !IsContentLockedOnElement() || targetPageIndex != selectedIndex;
    }

    private bool IsContentLockedOnElement()
    {
        foreach (var elementPosition in pagesPositions)
        {
            switch (orientation)
            {
                case UIPageViewControllerOrientation.Vertical:
                    if (IsContentLockedVerticaly(elementPosition)) return true;
                    break;
                case UIPageViewControllerOrientation.Horizontal:
                    if (IsContentLockedHorizontaly(elementPosition)) return true;
                    break;
            }
        }
        return false;
    }

    private bool IsContentLockedVerticaly(float elementPosition)
    {
        if (System.Math.Abs(elementPosition + contentPanel.transform.localPosition.y) < 1f)
        {
            selectedIndex = targetPageIndex;
            contentPanel.transform.localPosition = new Vector3(contentPanel.transform.localPosition.x, -elementPosition);
            return true;
        }
        return false;
    }

    private bool IsContentLockedHorizontaly(float elementPosition)
    {
        if (System.Math.Abs(elementPosition - contentPanel.transform.localPosition.x) < 1f)
        {
            selectedIndex = targetPageIndex;
            contentPanel.transform.localPosition = new Vector3(-elementPosition, contentPanel.transform.localPosition.y);
            return true;
        }

        return false;
    }


    private int GetTargetPageIndex()
    {
        return (orientation == UIPageViewControllerOrientation.Vertical) ?
            GetTargetVerticalPage() : GetTargetHorizontalPage();
    }

    private int GetTargetVerticalPage()
    {
        for (int i = 0; i < pagesPositions.Length; i++)
        {
            float firstPosition = (i == 0) ? 0 : pagesPositions[i - 1];
            float secondPosition = pagesPositions[i];

            float center = firstPosition + (secondPosition - firstPosition) / 2;

            if (contentPanel.transform.localPosition.y * -1 > center)
            {
                if (i > 0)
                {
                    return (i - 1);
                }
                else
                {
                    return 0;
                }
            }
        }

        return pagesPositions.Length - 1;
    }

    private int GetTargetHorizontalPage()
    {
        for (int i = 0; i < pagesPositions.Length; i++)
        {
            float firstPosition = (i == 0) ? 0 : pagesPositions[i - 1];
            float secondPosition = pagesPositions[i];

            float center = firstPosition + (secondPosition - firstPosition) / 2;
            if (contentPanel.transform.localPosition.x * -1 < center)
            {
                if (i > 0)
                {
                    return (i - 1);
                }
                else
                {
                    return 0;
                }
            }
        }

        return pagesPositions.Length - 1;
    }

    private void LerpToElement(int index)
    {
        Vector2 newPosition;
        if (orientation == UIPageViewControllerOrientation.Vertical)
        {
            float newPositionY = Mathf.Lerp(contentPanel.transform.localPosition.y, pagesPositions[index] * -1, Time.deltaTime * 15f);
            newPosition = new Vector2(contentPanel.transform.localPosition.x, newPositionY);

            contentPanel.transform.localPosition = newPosition;
        }
        else
        {
            float newPositionX = Mathf.Lerp(contentPanel.transform.localPosition.x, pagesPositions[index] * -1, Time.deltaTime * 15f);
            newPosition = new Vector2(newPositionX, contentPanel.transform.localPosition.y);

            contentPanel.transform.localPosition = newPosition;
        }
    }

    public void AddPage(GameObject page)
    {
        ConfigureAndAddPage(page);
        RecreateView();
    }

    public void AddPages(IEnumerable<GameObject> newPages)
    {
        foreach (var page in newPages)
        {
            ConfigureAndAddPage(page);
        }

        RecreateView();
    }

    public void AddUnselectablePrefixPages(IEnumerable<GameObject> prefixPages)
    {
        this.prefixPages.AddRange(prefixPages);
    }

    public void AddUnselectablePrefixPage(GameObject prefixPage)
    {
        prefixPages.Add(prefixPage);
    }

    public void AddUnselectableSufixPages(IEnumerable<GameObject> sufixPages)
    {
        this.sufixPages.AddRange(sufixPages);
    }

    public void AddUnselectableSufixPage(GameObject sufixPage)
    {
        sufixPages.Add(sufixPage);
    }

    private void ConfigureAndAddPage(GameObject page)
    {
        pages.Add(page);
        Button button;
        
        // If the gameobject already contains a button, just hook up to that one
        if (page.TryGetComponent<Button>(out var existingButton))
        {
            button = existingButton;
            button.onClick.AddListener(() => {
                JumpOnPage(pages.IndexOf(page));
            });
        }
        else
        {
            // If not, create a button
            button = page.AddComponent<Button>();
            button.onClick.AddListener(() => {
                JumpOnPage(pages.IndexOf(page));
            });
        }
    }

    public void JumpOnPage(int index)
    {
        if (selectedIndex == index) return;

        dragging = false;

        shouldSnap = true;

        selectedIndex = index;
        targetPageIndex = index;
    }

    public void PreselectPage(int index)
    {
        shouldSnap = true;
        targetPageIndex = index;
        _selectedIndex = index;
        OnSelectedPage?.Invoke(_selectedIndex, pages[_selectedIndex]);
        responderDelegate?.PageControllerSelectedPage(this, _selectedIndex, gameObject: pages[_selectedIndex]);
    }

    public void SetMargin(float margin)
    {
        this.margin = margin;
        RecreateView();
    }

    public void MakeVertical()
    {
        orientation = UIPageViewControllerOrientation.Vertical;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        RecreateView();
    }

    public void MakeHorizontal()
    {
        orientation = UIPageViewControllerOrientation.Horizontal;
        scrollRect.vertical = false;
        scrollRect.horizontal = true;
        RecreateView();
    }

    private void RecreateView()
    {
        float currentPosition = 0;

        pagesPositions = new float[pages.Count];

        for (int i = 0; i < pages.Count; i++)
        {
            pagesPositions[i] = currentPosition;
            var page = pages[i];

            var rect = SetPageParentAndGetRect(page);

            if (orientation == UIPageViewControllerOrientation.Vertical)
            {
                Vector3 newPosition = new Vector3(0, currentPosition, 0);

                rect.localPosition = newPosition;
                currentPosition -= rect.sizeDelta.y + margin;
            }
            else
            {
                Vector3 newPosition = new Vector3(currentPosition, 0, 0);

                rect.localPosition = newPosition;
                currentPosition += rect.sizeDelta.x + margin;
            }
        }
        RecreatePrefixAndSufixPages(currentPosition);
    }

    //TODO: Handle prefix and sufix for horizontal pages
    private void RecreatePrefixAndSufixPages(float currentPosition)
    {
        if (pages.Count < 1) return;
        float tempPosition = 0f;

        for (int i = 0; i < prefixPages.Count; i++)
        {
            var page = prefixPages[i];
            
            var rect = SetPageParentAndGetRect(page);

            if (orientation == UIPageViewControllerOrientation.Vertical)
            {
                tempPosition += rect.sizeDelta.y + margin;
            }
            else
            {

            }

            Vector3 newPosition = new Vector3(0, tempPosition, 0);
            rect.localPosition = newPosition;
        }

        tempPosition = currentPosition;
        for (int i = 0; i < sufixPages.Count; i++)
        {
            var page = sufixPages[i];

            var rect = SetPageParentAndGetRect(page);

            Vector3 newPosition = new Vector3(0, tempPosition, 0);
            rect.localPosition = newPosition;

            if (orientation == UIPageViewControllerOrientation.Vertical)
            {
                tempPosition -= rect.sizeDelta.y + margin;
            }
            else
            {

            }
        }
    }

    private RectTransform SetPageParentAndGetRect(GameObject page)
    {
        page.transform.SetParent(contentPanel.transform);
        page.transform.localScale = Vector3.one;

        return page.GetComponent<RectTransform>();
    }

    private float SizeOfScrollPanel()
    {
        //TODO: calculate from the sum of gameobjects and make public
        return 0;
    }

    public void StartDrag()
    {
        dragging = true;
    }

    public void EndDrag()
    {
        dragging = false;

        if (!IsContentLockedOnElement())
        {
            shouldSnap = true;
        }

        var newIndex = GetTargetPageIndex();
        if (newIndex == targetPageIndex) return;

        if (responderDelegate != null)
        {
            responderDelegate.PageControllerDeselectedPage(this, targetPageIndex, gameObject: pages[targetPageIndex]);
            responderDelegate.PageControllerSelectedPage(this, newIndex, gameObject: pages[targetPageIndex]);
        }

        targetPageIndex = newIndex;
    }

    public void Dragging()
    {
        var tempSelected = GetTargetPageIndex();
        if (selectedIndex != tempSelected)
        {
            selectedIndex = tempSelected;
            if (responderDelegate != null)
            {
                responderDelegate.PageControllerScrollingOverPage(this, selectedIndex, gameObject: pages[selectedIndex]);
            }
            OnScrollingOverPage?.Invoke(selectedIndex, pages[selectedIndex]);
        }
    }
}