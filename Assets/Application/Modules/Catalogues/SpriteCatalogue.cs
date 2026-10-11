using UnityEngine;

// Shared UI sprites, read through SpriteProvider. Add fields here as the game needs them.
[CreateAssetMenu(menuName = "Catalogues/Sprite Catalogue", fileName = "SpriteCatalogue")]
public class SpriteCatalogue : ScriptableObject
{
    public Sprite white;
    
    [Header("Images")]
    public Sprite item_Polaroid_Friends_Sprite;
    public Sprite item_Polaroid_Nonna_Sprite;
    public Sprite item_Cat_Sprite;
    public Sprite item_Childhood_Sprite;
}
