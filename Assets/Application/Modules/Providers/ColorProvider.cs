using UnityEngine;

public static class ColorProvider
{
    private static ColorCatalogue colors;

    public static ColorCatalogue Colors => colors != null ? colors : colors = CatalogueLoader.Load<ColorCatalogue>();
}
