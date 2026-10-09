#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

// ButtonEditor only draws Button's own fields, so UIButton's extras are drawn here.
[CustomEditor(typeof(UIButton), true)]
[CanEditMultipleObjects]
public class UIButtonEditor : ButtonEditor
{
    private const int FillSoftness = 12;
    private static readonly Color FillColor = new Color(0f, 0f, 0f, 0.25f);
    private static readonly string[] Fields = { "label", "clickSound", "clickCooldown", "pressedScale", "disabledLabelAlpha", "holdDuration" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        foreach (var field in Fields) EditorGUILayout.PropertyField(serializedObject.FindProperty(field));

        var holdDuration = serializedObject.FindProperty("holdDuration");
        var holdFill = serializedObject.FindProperty("holdFill");
        if (holdDuration.floatValue > 0f || holdDuration.hasMultipleDifferentValues)
        {
            EditorGUILayout.PropertyField(holdFill);
            if (holdFill.objectReferenceValue == null && !serializedObject.isEditingMultipleObjects && GUILayout.Button("Create Hold Fill"))
                holdFill.objectReferenceValue = CreateHoldFill((UIButton)target);
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }

    // HoldFill is a RectMask2D over the whole background; inside it a copy of the background
    // (same shape, e.g. FlexibleImage corners) is revealed left to right as the hold progresses.
    private static RectTransform CreateHoldFill(UIButton button)
    {
        var background = button.targetGraphic;
        if (background == null)
        {
            Debug.LogWarning("[UIButton] Assign a Target Graphic first - the hold fill is copied from it.", button);
            return null;
        }

        var container = new GameObject("HoldFill", typeof(RectTransform), typeof(RectMask2D));
        container.layer = background.gameObject.layer;
        var containerRect = (RectTransform)container.transform;
        containerRect.SetParent(background.transform, false);
        containerRect.SetAsFirstSibling();
        Stretch(containerRect);
        container.GetComponent<RectMask2D>().softness = new Vector2Int(FillSoftness, 0);

        var fill = Instantiate(background.gameObject, containerRect);
        for (var i = fill.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(fill.transform.GetChild(i).gameObject);

        fill.name = "Fill";
        Stretch((RectTransform)fill.transform);

        var graphic = fill.GetComponent<Graphic>();
        graphic.color = FillColor;
        graphic.raycastTarget = false;
        TintFlexibleImage(graphic);

        container.SetActive(false);
        Undo.RegisterCreatedObjectUndo(container, "Create Hold Fill");
        return containerRect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    // FlexibleImage ignores Graphic.color and draws its own primary colors, and it would also
    // copy the background's Selectable state animation - so recolor it and detach it.
    private static void TintFlexibleImage(Object graphic)
    {
        var serialized = new SerializedObject(graphic);
        var colors = serialized.FindProperty("_instanceQuadDataContainer._quadDataList.Array.data[0]" +
            ".proceduralAnimationStates.Array.data[0].proceduralProperties.Array.data[0].primaryColors");
        if (colors == null) return;

        for (var i = 0; i < colors.arraySize; i++) colors.GetArrayElementAtIndex(i).colorValue = FillColor;

        var selectable = serialized.FindProperty("selectable");
        if (selectable != null) selectable.objectReferenceValue = null;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
