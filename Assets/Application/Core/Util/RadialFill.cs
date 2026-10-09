using UnityEngine;

// Drives a shader-based radial fill (e.g. for a cooldown ring or progress
// indicator). Requires fillRenderer's material to expose a float property
// matching parameterName (default "_Arc2") — generic beyond that.
public class RadialFill : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private SpriteRenderer backgroundRenderer;

    private Material fillMaterial;

    public void Setup(Color color)
    {
        fillMaterial = fillRenderer.material;
        fillRenderer.color = color;
        ChangeFill(360f);
    }

    public void UpdateFill(float currentValue, float targetValue)
    {
        ChangeFill(Mathf.Lerp(360, 0, currentValue / targetValue));
    }

    public void ChangeFill(float value, string parameterName = "_Arc2")
    {
        fillMaterial.SetFloat(parameterName, value);
    }

    public void Cleanup() => ChangeFill(360f);
}
