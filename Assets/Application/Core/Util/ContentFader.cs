using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Fades a set of Inspector-assigned texts/images between full opacity and a
// dimmed state — e.g. for a "locked" UI panel.
public class ContentFader : MonoBehaviour
{
    [SerializeField] private List<TextMeshProUGUI> texts;
    [SerializeField] private List<Image> images;
    [SerializeField] private float disabledAlpha = 0.5f;

    private bool isFaded;

    public void Fade(bool fade)
    {
        if (isFaded == fade) return;

        foreach (var text in texts)
        {
            text.color = new Color(text.color.r, text.color.g, text.color.b, fade ? disabledAlpha : 1);
        }

        foreach (var image in images)
        {
            image.color = new Color(image.color.r, image.color.g, image.color.b, fade ? disabledAlpha : 1);
        }

        isFaded = fade;
    }
}
