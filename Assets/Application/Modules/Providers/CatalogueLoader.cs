using UnityEngine;

// Catalogues live in Resources/AssetProviders/ named after their type, e.g. ColorCatalogue.asset.
// A missing one logs an error and falls back to defaults so callers never get null.
public static class CatalogueLoader
{
    public static T Load<T>() where T : ScriptableObject
    {
        var path = Strings.AssetProvidersPath + typeof(T).Name;
        var catalogue = Resources.Load<T>(path);
        if (catalogue != null) return catalogue;

        Debug.LogError($"[CatalogueLoader] Missing Resources/{path}.asset - using defaults.");
        return ScriptableObject.CreateInstance<T>();
    }
}
