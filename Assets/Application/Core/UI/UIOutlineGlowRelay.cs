using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Added at runtime onto a Selectable by UIOutlineGlow, so a glow that is not an ancestor of the
// button still follows its highlight.
[DisallowMultipleComponent]
public class UIOutlineGlowRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UIOutlineGlow glow;
    private Selectable selectable;

    public static void Attach(Selectable selectable, UIOutlineGlow glow)
    {
        if (selectable == null || glow == null) return;

        var relay = selectable.GetComponent<UIOutlineGlowRelay>();
        if (relay == null) relay = selectable.gameObject.AddComponent<UIOutlineGlowRelay>();
        relay.selectable = selectable;
        relay.glow = glow;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (glow != null && (selectable == null || selectable.IsInteractable())) glow.SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (glow != null) glow.SetHighlighted(false);
    }
}
