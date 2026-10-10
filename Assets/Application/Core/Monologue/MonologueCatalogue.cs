using System;
using System.Collections.Generic;
using UnityEngine;

// Loaded from Resources/AssetProviders/MonologueCatalogue by MonologueService.
[CreateAssetMenu(menuName = "Catalogues/Monologue Catalogue", fileName = "MonologueCatalogue")]
public class MonologueCatalogue : ScriptableObject
{
    [Tooltip("Default box for every monologue: a prefab with a MonologueView on its root. " +
             "Instantiated once on first use and kept across scene loads.")]
    public GameObject viewPrefab;

    [Tooltip("Sorting order given to the view's own Canvas, so the box stays above the game's UI.")]
    public int canvasSortOrder = 500;

    [Header("Monologues")]
    public List<MonologueDefinition> monologues = new List<MonologueDefinition>();

    [NonSerialized] private Dictionary<string, MonologueDefinition> lookup;

    public bool TryGet(string id, out MonologueDefinition monologue)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id ?? string.Empty, out monologue);
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, MonologueDefinition>(monologues.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var monologue in monologues)
        {
            if (monologue != null && !lookup.TryAdd(monologue.Id, monologue))
                Debug.LogWarning($"[MonologueCatalogue] Duplicate monologue id \"{monologue.Id}\" - only the first one is used.", this);
        }
    }

    private void OnValidate() => lookup = null;
}
