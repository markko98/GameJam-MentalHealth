using UnityEngine;
using UnityEngine.Serialization;

// Shared UI palette, read through ColorProvider. Add fields here as the game needs them.
[CreateAssetMenu(menuName = "Catalogues/Color Catalogue", fileName = "ColorCatalogue")]
public class ColorCatalogue : ScriptableObject
{
    [Header("Text")]
    [FormerlySerializedAs("buttonTextColor")] public Color text = Color.white;
    [FormerlySerializedAs("highlightTextColor")] public Color highlight = Color.yellow;

    [Header("States")]
    [FormerlySerializedAs("tabActiveColor")] public Color active = Color.white;
    [FormerlySerializedAs("tabInactiveColor")] public Color inactive = Color.gray;
    [FormerlySerializedAs("tabDisabledColor")] public Color disabled = new Color(1f, 1f, 1f, 0.4f);
}
