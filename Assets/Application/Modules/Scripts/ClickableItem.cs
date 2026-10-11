using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Put this on anything clickable: it raises ItemClickedEvent with its id, so one listener can
// handle every item instead of wiring each one.
[DisallowMultipleComponent]
public class ClickableItem : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Id sent with the event. Empty = the GameObject's name.")]
    [SerializeField] private string id;
    [SerializeField] private Button button;
    [SerializeField] private UnityEvent onClick;

    public string Id => string.IsNullOrWhiteSpace(id) ? name : id.Trim();
    public bool Clickable { get; set; } = true;

    // Works when the item's button is a UIButton (see UIButton.SetHighlighted).
    public void SetHighlighted(bool highlighted)
    {
        if (button is UIButton uiButton) uiButton.SetHighlighted(highlighted);
    }

    public void Click()
    {
        if (!Clickable) return;
        if (TryGetComponent<Selectable>(out var selectable) && !selectable.IsInteractable()) return;

        onClick?.Invoke();
        UEventBus<ItemClickedEvent>.Raise(new ItemClickedEvent(Id, gameObject));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null) Click();
    }

    private void Awake()
    {
        if (button == null) button = GetComponentInChildren<Button>(true);
        if (button != null) button.onClick.AddListener(Click);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Click);
    }
}
