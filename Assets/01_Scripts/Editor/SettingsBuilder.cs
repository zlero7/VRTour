using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메뉴 [VRTour > 설정 패널 만들기 (현재 씬)] 으로 머리에 붙는 설정 패널을 만들고,
/// 상단 MainUI(집으로 / 이동UI)에 [설정] 버튼을 추가. 다시 실행하면 기존 것을 지우고 새로 만듦.
/// [VRTour > 타이틀 + 설정 전체 씬에 적용] 은 Main(타이틀)과 Scene1~3을 한 번에 처리하고 저장.
/// </summary>
public static class SettingsBuilder
{
    internal const string RootName = "Settings_Canvas";
    private const string ButtonName = "SettingsBtn";

    private static readonly string[] ScenePaths =
    {
        "Assets/00_Scenes/Main.unity",
        "Assets/00_Scenes/Scene1.unity",
        "Assets/00_Scenes/Scene2.unity",
        "Assets/00_Scenes/Scene3.unity",
    };

    private static readonly Vector2 PanelSize = new Vector2(360f, 330f);
    private static readonly Color Dark = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color Ink = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color Green = new Color(0.55f, 0.85f, 0.6f);

    [MenuItem("VRTour/설정 패널 만들기 (현재 씬)")]
    private static void BuildFromMenu()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name == "Main") TitleBuilder.Build(scene);
        else Build(scene, true);
        StampBuilder.Build(scene);
    }

    [MenuItem("VRTour/타이틀 + 설정 전체 씬에 적용")]
    private static void BuildAll()
    {
        foreach (string path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (scene.name == "Main") TitleBuilder.Build(scene);
            else Build(scene, true);
            StampBuilder.Build(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("[SettingsBuilder] Main, Scene1~3에 적용하고 저장했습니다.");
    }

    internal static Transform Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    internal static SettingsPanel Build(Scene scene, bool addButton)
    {
        PharmacyBuilder.LoadUiAssets();

        var old = Find(scene, RootName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var cam = Find(scene, "Main Camera");
        var moveUI = Find(scene, "MoveUI");
        if (cam == null || moveUI == null)
        {
            Debug.LogError($"[SettingsBuilder] {scene.name}: Main Camera 또는 MoveUI를 찾지 못했습니다.");
            return null;
        }

        // 이동 UI와 같은 자리, 같은 크기 비율로 배치
        var canvas = PharmacyBuilder.MakeCanvas(RootName, cam, moveUI.position, PanelSize, moveUI.lossyScale.x);
        var settings = canvas.gameObject.AddComponent<SettingsPanel>();

        var audio = canvas.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PharmacyBuilder.SfxPath);
        settings.preview = audio;

        var panel = PharmacyBuilder.MakePanel("Panel", canvas, Vector2.zero, PanelSize, Dark);
        settings.panel = panel.gameObject;

        MakeLabel(panel, "Title", "설정", new Vector2(0f, 130f), new Vector2(200f, 40f), 30f, TextAlignmentOptions.Center);
        settings.closeButton = PharmacyBuilder.MakeButton("CloseBtn", panel, "X", new Vector2(150f, 130f), new Vector2(44f, 36f), new Color(0.95f, 0.55f, 0.5f));
        StyleLabel(settings.closeButton, 22f);

        MakeLabel(panel, "BgmLabel", "배경음", new Vector2(-80f, 78f), new Vector2(180f, 30f), 22f, TextAlignmentOptions.Left);
        settings.bgmValue = MakeLabel(panel, "BgmValue", "100%", new Vector2(130f, 78f), new Vector2(80f, 30f), 22f, TextAlignmentOptions.Right);
        settings.bgmSlider = MakeSlider(panel, "BgmSlider", new Vector2(0f, 48f), new Vector2(310f, 26f));

        MakeLabel(panel, "SfxLabel", "효과음", new Vector2(-80f, 8f), new Vector2(180f, 30f), 22f, TextAlignmentOptions.Left);
        settings.sfxValue = MakeLabel(panel, "SfxValue", "100%", new Vector2(130f, 8f), new Vector2(80f, 30f), 22f, TextAlignmentOptions.Right);
        settings.sfxSlider = MakeSlider(panel, "SfxSlider", new Vector2(0f, -22f), new Vector2(310f, 26f));

        settings.moveButton = PharmacyBuilder.MakeButton("MoveBtn", panel, "", new Vector2(0f, -88f), new Vector2(320f, 56f), new Color(1f, 0.9f, 0.45f));
        StyleLabel(settings.moveButton, 22f);
        settings.moveLabel = settings.moveButton.GetComponentInChildren<TMP_Text>();
        MakeLabel(panel, "Hint", "버튼을 눌러 이동 방식을 바꿀 수 있어요", new Vector2(0f, -138f), new Vector2(330f, 26f), 15f, TextAlignmentOptions.Center);

        // 같은 자리에 뜨는 다른 메뉴(이동하기, 맵 이동)는 설정을 열 때 숨김
        var hide = new System.Collections.Generic.List<GameObject> { moveUI.gameObject };
        var mapTp = Find(scene, "MapTPUI");
        if (mapTp != null) hide.Add(mapTp.gameObject);
        settings.hideWhileOpen = hide.ToArray();

        if (addButton) AddSettingsButton(scene, settings);

        EditorUtility.SetDirty(settings);
        EditorSceneManager.MarkSceneDirty(scene);
        return settings;
    }

    // 상단 MainUI의 [이동UI] 버튼을 복제해 [설정] 버튼으로 만들고, 세 버튼이 나란히 들어가게 패널을 넓힘
    private static void AddSettingsButton(Scene scene, SettingsPanel settings)
    {
        var oldBtn = Find(scene, ButtonName);
        if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

        var openBtn = Find(scene, "UIOpenBtn");
        var homeBtn = Find(scene, "HomeBtn");
        var mainUI = (RectTransform)Find(scene, "MainUI");
        var main = (RectTransform)Find(scene, "Main");
        if (openBtn == null || homeBtn == null || mainUI == null || main == null)
        {
            Debug.LogWarning($"[SettingsBuilder] {scene.name}: MainUI/HomeBtn/UIOpenBtn을 찾지 못해 [설정] 버튼은 건너뜁니다.");
            return;
        }

        var clone = Object.Instantiate(openBtn.gameObject, openBtn.parent);
        clone.name = ButtonName;
        var button = clone.GetComponent<Button>();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        clone.GetComponentInChildren<TMP_Text>(true).text = "설정";
        UnityEventTools.AddPersistentListener(button.onClick, settings.Toggle);
        var sfx = clone.GetComponent<SfxPlayer>();
        if (sfx != null) UnityEventTools.AddPersistentListener(button.onClick, sfx.Play);

        // 버튼들을 같은 간격으로 가운데 정렬: 집으로 / 이동UI / (맵이동UI) / 설정
        var buttons = new System.Collections.Generic.List<Transform> { homeBtn, openBtn };
        var mapBtn = Find(scene, "MapTPUIBtn");
        if (mapBtn != null) buttons.Add(mapBtn);
        buttons.Add(clone.transform);

        const float step = 68.79f;
        float width = 145.27f + (buttons.Count - 2) * step; // 버튼 2개일 때 원래 폭
        mainUI.sizeDelta = new Vector2(width, mainUI.sizeDelta.y);
        main.sizeDelta = new Vector2(width - 0.01f, main.sizeDelta.y);
        // Main의 anchor가 왼쪽 위라서, 폭을 바꾼 뒤 가운데 오도록 위치를 다시 잡아야 함
        main.anchoredPosition = new Vector2(width / 2f, main.anchoredPosition.y);

        // 크기를 바꾼 뒤에 위치를 정해야 버튼이 패널 안에 들어옴
        for (int i = 0; i < buttons.Count; i++)
            SetX((RectTransform)buttons[i], width / 2f + (i - (buttons.Count - 1) / 2f) * step);
    }

    // 왼쪽 위 anchor 기준 x 좌표. localPosition은 방금 연 씬에서 값이 틀어져서 anchoredPosition을 씀
    private static void SetX(RectTransform rt, float anchoredX)
    {
        rt.anchoredPosition = new Vector2(anchoredX, rt.anchoredPosition.y);
    }

    private static TextMeshProUGUI MakeLabel(RectTransform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        var tmp = PharmacyBuilder.MakeUIText(name, parent, text, pos, size, fontSize, Color.white);
        tmp.alignment = align;
        return tmp;
    }

    // MakeButton 기본 글자 크기(50)를 UI 크기에 맞게 줄임
    private static void StyleLabel(Button button, float fontSize)
    {
        var tmp = button.GetComponentInChildren<TMP_Text>();
        tmp.fontSizeMax = fontSize;
        tmp.fontSizeMin = fontSize * 0.6f;
        tmp.fontSize = fontSize;
    }

    private static Slider MakeSlider(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        var rt = (RectTransform)root.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // 트랙(배경)과 채워지는 부분. 트랙 높이는 12로 고정
        var track = PharmacyBuilder.MakePanel("Background", rt, Vector2.zero, new Vector2(size.x, 12f), new Color(1f, 1f, 1f, 0.25f));

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        var fillAreaRt = (RectTransform)fillArea.transform;
        fillAreaRt.SetParent(rt, false);
        fillAreaRt.anchoredPosition = Vector2.zero;
        fillAreaRt.sizeDelta = new Vector2(size.x, 12f);
        var fill = PharmacyBuilder.MakePanel("Fill", fillAreaRt, Vector2.zero, Vector2.zero, Green);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0.5f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        // 손잡이: 슬라이더가 anchor x를 움직임
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        var handleAreaRt = (RectTransform)handleArea.transform;
        handleAreaRt.SetParent(rt, false);
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(12f, 0f);
        handleAreaRt.offsetMax = new Vector2(-12f, 0f);
        var handle = PharmacyBuilder.MakePanel("Handle", handleAreaRt, Vector2.zero, new Vector2(24f, 24f), Color.white);
        handle.anchorMin = new Vector2(0.5f, 0.5f);
        handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.anchoredPosition = Vector2.zero;

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }
}
