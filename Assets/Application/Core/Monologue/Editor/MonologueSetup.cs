#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Builds the view prefab, the catalogue and an example monologue.
public static class MonologueSetup
{
    private const string PrefabPath = "Assets/Application/Prefabs/UI/UI_MonologueView.prefab";
    private const string CataloguePath = "Assets/Application/Resources/AssetProviders/MonologueCatalogue.asset";
    private const string DemoPath = "Assets/Application/Core/Monologue/Demo/Monologue_Demo.asset";
    private const string TextPrefabPath = "Assets/Application/Prefabs/UI/UI_Text.prefab";
    private const string ColorCataloguePath = "Assets/Application/Resources/AssetProviders/ColorCatalogue.asset";

    [MenuItem("Tools/Create/Monologue System")]
    public static void Setup()
    {
        var prefab = CreateViewPrefab();
        var catalogue = ScriptableObjectAssetUtility.CreateAssetIfMissing<MonologueCatalogue>(CataloguePath);
        var demo = CreateDemoMonologue();

        Undo.RecordObject(catalogue, "Monologue Setup");
        if (catalogue.viewPrefab == null) catalogue.viewPrefab = prefab;
        catalogue.monologues.RemoveAll(monologue => monologue == null);
        if (demo != null && !catalogue.monologues.Contains(demo)) catalogue.monologues.Add(demo);

        foreach (var definition in AssetDatabase.FindAssets("t:MonologueDefinition")
                     .Select(guid => AssetDatabase.LoadAssetAtPath<MonologueDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                     .Where(definition => definition != null && !catalogue.monologues.Contains(definition)))
        {
            catalogue.monologues.Add(definition);
        }

        EditorUtility.SetDirty(catalogue);
        AssetDatabase.SaveAssets();
        MonologueIdsCodeGenerator.Generate(catalogue);

        Selection.activeObject = catalogue;
        EditorGUIUtility.PingObject(catalogue);
        Debug.Log($"[MonologueSetup] Ready. View prefab: {PrefabPath}, catalogue: {CataloguePath}. " +
                  "Play a monologue with MonologueService.Play(MonologueIds.<Id>).", catalogue);
    }

    #region View prefab

    [MenuItem("Tools/Create/Monologue View Prefab")]
    private static void CreateViewPrefabMenu() => Selection.activeObject = CreateViewPrefab();

    private static GameObject CreateViewPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        ScriptableObjectAssetUtility.EnsureFolder("Assets/Application/Prefabs/UI");

        var colors = AssetDatabase.LoadAssetAtPath<ColorCatalogue>(ColorCataloguePath);
        var textColor = colors != null ? colors.text : Color.white;
        var highlightColor = colors != null ? colors.highlight : new Color(0.98f, 0.76f, 0.36f);

        var root = new GameObject("UI_MonologueView",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(AudioSource));

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var voice = root.GetComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.loop = false;
        var soundCatalog = LoadFirst<SoundCatalog>();
        if (soundCatalog != null) voice.outputAudioMixerGroup = soundCatalog.GetGroup(SoundChannel.UI);

        var tapArea = CreateChild(root.transform, "TapArea", typeof(Image), typeof(Button));
        Stretch((RectTransform)tapArea.transform);
        var tapImage = tapArea.GetComponent<Image>();
        tapImage.color = new Color(0f, 0f, 0f, 0f);
        tapImage.raycastTarget = true;
        var tapButton = tapArea.GetComponent<Button>();
        tapButton.transition = Selectable.Transition.None;

        var panel = CreateChild(root.transform, "Panel", typeof(Image));
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(1320f, 300f);
        panelRect.anchoredPosition = new Vector2(0f, 90f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.14f, 0.12f, 0.17f, 0.92f);
        panelImage.raycastTarget = false;

        var portrait = CreateChild(panel.transform, "Portrait", typeof(Image));
        var portraitRect = (RectTransform)portrait.transform;
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.sizeDelta = new Vector2(200f, 200f);
        portraitRect.anchoredPosition = new Vector2(44f, 0f);
        var portraitImage = portrait.GetComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        portrait.SetActive(false);

        var speaker = CreateLabel(panel.transform, "Speaker", 34f, highlightColor, TextAlignmentOptions.TopLeft);
        var speakerRect = (RectTransform)speaker.transform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.offsetMin = new Vector2(56f, -76f);
        speakerRect.offsetMax = new Vector2(-56f, -24f);
        speaker.gameObject.SetActive(false);

        var body = CreateLabel(panel.transform, "Text", 40f, textColor, TextAlignmentOptions.TopLeft);
        var bodyRect = (RectTransform)body.transform;
        Stretch(bodyRect);
        bodyRect.offsetMin = new Vector2(56f, 56f);
        bodyRect.offsetMax = new Vector2(-56f, -84f);

        var subtitles = body.gameObject.AddComponent<UISubtitles>();
        var subtitlesObject = new SerializedObject(subtitles);
        subtitlesObject.FindProperty("label").objectReferenceValue = body;
        subtitlesObject.FindProperty("voiceSource").objectReferenceValue = voice;
        subtitlesObject.ApplyModifiedPropertiesWithoutUndo();

        var indicator = CreateLabel(panel.transform, "ContinueIndicator", 40f, highlightColor, TextAlignmentOptions.BottomRight);
        indicator.Text = "▾";
        var indicatorRect = (RectTransform)indicator.transform;
        indicatorRect.anchorMin = new Vector2(1f, 0f);
        indicatorRect.anchorMax = new Vector2(1f, 0f);
        indicatorRect.pivot = new Vector2(1f, 0f);
        indicatorRect.sizeDelta = new Vector2(60f, 60f);
        indicatorRect.anchoredPosition = new Vector2(-36f, 20f);
        indicator.gameObject.SetActive(false);

        var view = root.AddComponent<MonologueView>();
        var viewObject = new SerializedObject(view);
        viewObject.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
        viewObject.FindProperty("panel").objectReferenceValue = panelRect;
        viewObject.FindProperty("subtitles").objectReferenceValue = subtitles;
        viewObject.FindProperty("speakerLabel").objectReferenceValue = speaker;
        viewObject.FindProperty("portrait").objectReferenceValue = portraitImage;
        viewObject.FindProperty("continueIndicator").objectReferenceValue = indicator.gameObject;
        viewObject.FindProperty("tapArea").objectReferenceValue = tapButton;
        viewObject.FindProperty("canvas").objectReferenceValue = canvas;
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
    {
        var child = new GameObject(name, components);
        var rect = child.GetComponent<RectTransform>();
        if (rect == null) rect = child.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        return child;
    }

    private static UILabel CreateLabel(Transform parent, string name, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var textPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TextPrefabPath);

        GameObject instance;
        if (textPrefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(textPrefab, parent);
        }
        else
        {
            instance = new GameObject(name, typeof(TextMeshProUGUI));
            instance.transform.SetParent(parent, false);
        }

        instance.name = name;

        var tmp = instance.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.text = string.Empty;
        }

        return instance.GetComponent<UILabel>() ?? instance.AddComponent<UILabel>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    #endregion

    #region Example monologue

    private static MonologueDefinition CreateDemoMonologue()
    {
        var existing = AssetDatabase.LoadAssetAtPath<MonologueDefinition>(DemoPath);
        if (existing != null) return existing;

        var demo = ScriptableObjectAssetUtility.CreateAssetIfMissing<MonologueDefinition>(DemoPath);
        demo.speaker = "Glas";
        demo.lines.Add(new MonologueLine("Bok. Drago mi je što si tu."));
        demo.lines.Add(new MonologueLine("Ovo su titlovi: tipkaju se, ostanu dovoljno dugo za čitanje i nestanu."));
        demo.lines.Add(new MonologueLine("Klikni bilo gdje da preskočiš tipkanje ili prijeđeš na sljedeću liniju."));
        EditorUtility.SetDirty(demo);
        return demo;
    }

    private static T LoadFirst<T>() where T : ScriptableObject
    {
        var guid = AssetDatabase.FindAssets($"t:{typeof(T).Name}").FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }

    #endregion
}
#endif
