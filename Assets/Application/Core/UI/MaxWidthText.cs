using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Caps the text's preferred width so it wraps into new lines past maxWidth. Height is left to TMP,
// which measures it for the width the layout group actually gives the text (Control Child Size on).
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class MaxWidthText : UIBehaviour, ILayoutElement
{
    [SerializeField, Min(0f)] private float maxWidth = 1200f;
    [Tooltip("Must beat TMP (0) and LayoutElement (1).")]
    [SerializeField] private int priority = 2;

    private TMP_Text text;

    private TMP_Text Text => text != null ? text : text = GetComponent<TMP_Text>();

    public float preferredWidth => Mathf.Min(Text.preferredWidth, maxWidth);
    public float preferredHeight => -1f;
    public float minWidth => -1f;
    public float minHeight => -1f;
    public float flexibleWidth => -1f;
    public float flexibleHeight => -1f;
    public int layoutPriority => priority;

    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }

    protected override void OnEnable()
    {
        base.OnEnable();
        LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (isActiveAndEnabled) LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
    }
#endif
}
