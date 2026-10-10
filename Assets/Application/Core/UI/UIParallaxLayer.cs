using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Added to interactable parallax layers. It is a Selectable, so a FlexibleImage can animate its
// own hover/press states, and it reports clicks and hover back to the UIParallax.
[DisallowMultipleComponent]
public class UIParallaxLayer : Selectable, IPointerClickHandler
{
    [HideInInspector] public UIParallax parallax;
    [HideInInspector] public int index;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (IsInteractable() && parallax != null) parallax.NotifyHover(index, true);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if (parallax != null) parallax.NotifyHover(index, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsInteractable() && parallax != null) parallax.NotifyClick(index);
    }
}
