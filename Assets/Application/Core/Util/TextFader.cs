using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Fades a button's own texts/images to match its interactable state. Call
// Refresh() after changing button.interactable (also runs once on enable) —
// event-driven rather than polling every frame.
[RequireComponent(typeof(Button))]
public class TextFader : MonoBehaviour
{
    [SerializeField] private float disabledAlpha = 0.5f;

    private Button button;
    private List<TextMeshProUGUI> texts;
    private List<Image> images;

    private void Awake()
    {
        button = GetComponent<Button>();
        texts = GetComponentsInChildren<TextMeshProUGUI>(true).ToList();
        images = GetComponentsInChildren<Image>(true).Where(i => i.gameObject != gameObject).ToList();
    }

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        var alpha = button.interactable ? 1f : disabledAlpha;

        foreach (var text in texts)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
        }

        foreach (var image in images)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }
    }
}
