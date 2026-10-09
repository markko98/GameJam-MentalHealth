using UnityEngine;

public static class SpriteProvider
{
    private static SpriteCatalogue sprites;

    public static SpriteCatalogue Sprites => sprites != null ? sprites : sprites = CatalogueLoader.Load<SpriteCatalogue>();
}
