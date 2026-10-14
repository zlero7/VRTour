using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main 씬에 타이틀 화면(제목 + [투어 시작] / [장소 선택] / [설정])을 만들고 설정 패널을 붙임.
/// 기존 "이동하기" 메뉴(MoveUI)는 [장소 선택]을 눌렀을 때만 나오게 하고, 닫기(X) 버튼을 추가.
/// SettingsBuilder 메뉴에서 실행. 다시 실행하면 기존 것을 지우고 새로 만듦.
/// </summary>
public static class TitleBuilder
{
    private const string RootName = "Title_Canvas";
    private const string BackButtonName = "BackToTitleBtn";

    private static readonly Vector3 CanvasPos = new Vector3(0f, 1.4f, -8.9f);
    private const float CanvasScale = 0.0012f;

    private static readonly Color Ink = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color Cream = new Color(1f, 0.95f, 0.75f);

    internal static void Build(Scene scene)
    {
        PharmacyBuilder.LoadUiAssets();

        var old = SettingsBuilder.Find(scene, RootName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var moveUI = SettingsBuilder.Find(scene, "MoveUI");
        if (moveUI == null)
        {
            Debug.LogError("[TitleBuilder] Main 씬에서 MoveUI를 찾지 못했습니다.");
            return;
        }

        // 설정 패널 (Main에는 상단 MainUI가 없으니 버튼은 타이틀에 따로 둠)
        var settings = SettingsBuilder.Build(scene, false);
        if (settings == null) return;

        var canvas = PharmacyBuilder.MakeCanvas(RootName, null, CanvasPos, new Vector2(1000f, 700f), CanvasScale);
        SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);
        var title = canvas.gameObject.AddComponent<TitleScreen>();

        var panel = PharmacyBuilder.MakePanel("Title_Panel", canvas, Vector2.zero, new Vector2(1000f, 700f), new Color(0f, 0f, 0f, 0.6f));
        title.titlePanel = panel.gameObject;
        title.titleGroup = panel.gameObject.AddComponent<CanvasGroup>();

        // 둥실거리는 제목 묶음
        var block = new GameObject("Title_Block", typeof(RectTransform));
        var blockRt = (RectTransform)block.transform;
        blockRt.SetParent(panel, false);
        blockRt.anchoredPosition = new Vector2(0f, 150f);
        blockRt.sizeDelta = new Vector2(900f, 240f);
        title.titleBlock = blockRt;
        PharmacyBuilder.MakeUIText("Tag", blockRt, "VR 360° 투어 체험", new Vector2(0f, 95f), new Vector2(700f, 50f), 40f, new Color(1f, 0.85f, 0.4f));
        PharmacyBuilder.MakeUIText("Title", blockRt, "불광동 먹자골목 투어", new Vector2(0f, 15f), new Vector2(940f, 120f), 96f, Color.white);
        PharmacyBuilder.MakeUIText("Subtitle", blockRt, "카페에서 주문하고, 약국에서 약을 찾아보세요!", new Vector2(0f, -75f), new Vector2(940f, 60f), 38f, Cream);

        title.startButton = PharmacyBuilder.MakeButton("StartBtn", panel, "투어 시작", new Vector2(0f, -130f), new Vector2(520f, 120f), new Color(0.55f, 0.85f, 0.6f));
        // 아래 줄 버튼 4개: 장소 선택 / 스탬프 / 내 사진 / 설정
        var bottom = new Vector2(225f, 100f);
        title.placesButton = PharmacyBuilder.MakeButton("PlacesBtn", panel, "장소 선택", new Vector2(-345f, -260f), bottom, new Color(0.98f, 0.8f, 0.45f));
        title.stampsButton = PharmacyBuilder.MakeButton("StampsBtn", panel, "스탬프", new Vector2(-115f, -260f), bottom, new Color(0.95f, 0.65f, 0.7f));
        title.photosButton = PharmacyBuilder.MakeButton("PhotosBtn", panel, "내 사진", new Vector2(115f, -260f), bottom, new Color(0.6f, 0.8f, 1f));
        title.settingsButton = PharmacyBuilder.MakeButton("SettingsBtn", panel, "설정", new Vector2(345f, -260f), bottom, new Color(0.85f, 0.85f, 0.9f));
        BuildStampPanel(canvas, title);
        BuildPhotoPanel(canvas, title);
        title.startScene = "Scene2";
        title.placesMenu = moveUI.gameObject;
        title.settings = settings;

        AddBackButton(moveUI, title);

        Undo.RegisterCreatedObjectUndo(canvas.gameObject, "타이틀 화면 만들기");
        EditorUtility.SetDirty(title);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[TitleBuilder] 타이틀 화면 배치 완료.");
    }

    // 스탬프 판: 스탬프 4칸(2x2) + 요약 + 돌아가기 버튼. 타이틀 패널과 같은 자리에 겹쳐 두고 평소엔 숨김
    private static void BuildStampPanel(RectTransform canvas, TitleScreen title)
    {
        var panel = PharmacyBuilder.MakePanel("Stamp_Panel", canvas, Vector2.zero, new Vector2(1000f, 700f), new Color(0f, 0f, 0f, 0.6f));
        title.stampPanel = panel.gameObject;
        var board = panel.gameObject.AddComponent<StampBoard>();

        PharmacyBuilder.MakeUIText("Title", panel, "나의 스탬프", new Vector2(0f, 285f), new Vector2(800f, 90f), 72f, Color.white);

        int n = StampBook.Names.Length;
        board.cellBackgrounds = new UnityEngine.UI.Image[n];
        board.cellLabels = new TextMeshProUGUI[n];
        for (int i = 0; i < n; i++)
        {
            var pos = new Vector2(i % 2 == 0 ? -240f : 240f, i / 2 == 0 ? 120f : -60f);
            var cell = PharmacyBuilder.MakePanel($"Stamp_{i}", panel, pos, new Vector2(440f, 150f), Color.gray);
            board.cellBackgrounds[i] = cell.GetComponent<UnityEngine.UI.Image>();
            board.cellLabels[i] = PharmacyBuilder.MakeUIText("Label", cell, "", Vector2.zero, new Vector2(410f, 130f), 40f, Ink);
        }
        board.summary = PharmacyBuilder.MakeUIText("Summary", panel, "", new Vector2(0f, -190f), new Vector2(900f, 60f), 40f, Cream);
        title.stampCloseButton = PharmacyBuilder.MakeButton("StampCloseBtn", panel, "돌아가기", new Vector2(0f, -285f), new Vector2(360f, 90f), new Color(0.98f, 0.8f, 0.45f));
        panel.gameObject.SetActive(false);
    }

    // 내 사진: 최근 사진 3장 + 안내 + 돌아가기. 스탬프 판과 같은 자리에 겹쳐 두고 평소엔 숨김
    private static void BuildPhotoPanel(RectTransform canvas, TitleScreen title)
    {
        var panel = PharmacyBuilder.MakePanel("Photo_Panel", canvas, Vector2.zero, new Vector2(1000f, 700f), new Color(0f, 0f, 0f, 0.6f));
        title.photoPanel = panel.gameObject;
        var gallery = panel.gameObject.AddComponent<PhotoGallery>();

        PharmacyBuilder.MakeUIText("Title", panel, "내 사진", new Vector2(0f, 285f), new Vector2(800f, 90f), 72f, Color.white);

        gallery.slots = new RawImage[3];
        for (int i = 0; i < 3; i++)
        {
            var frame = PharmacyBuilder.MakePanel($"Frame_{i}", panel, new Vector2(-310f + 310f * i, 60f), new Vector2(290f, 190f), Color.white);
            var go = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(frame, false);
            rt.sizeDelta = new Vector2(266f, 150f);
            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.enabled = false;
            gallery.slots[i] = raw;
        }

        gallery.emptyText = PharmacyBuilder.MakeUIText("Empty", panel, "아직 찍은 사진이 없어요", new Vector2(0f, 60f), new Vector2(800f, 80f), 52f, Cream);
        PharmacyBuilder.MakeUIText("Hint", panel, "어디서든 컨트롤러 A 버튼(키보드 P)을 누르면 사진을 찍어요", new Vector2(0f, -120f), new Vector2(920f, 60f), 38f, Cream);
        title.photoCloseButton = PharmacyBuilder.MakeButton("PhotoCloseBtn", panel, "돌아가기", new Vector2(0f, -250f), new Vector2(360f, 90f), new Color(0.98f, 0.8f, 0.45f));
        panel.gameObject.SetActive(false);
    }

    // 이동하기 메뉴 오른쪽 위에 닫기(X) 버튼: 눌러서 타이틀로 돌아옴
    private static void AddBackButton(Transform moveUI, TitleScreen title)
    {
        var old = SettingsBuilder.Find(moveUI.gameObject.scene, BackButtonName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var src = SettingsBuilder.Find(moveUI.gameObject.scene, "GoLastSceneBtn");
        if (src == null) return;

        var clone = Object.Instantiate(src.gameObject, src.parent);
        clone.name = BackButtonName;
        var button = clone.GetComponent<Button>();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, title.ShowTitle);

        clone.GetComponentInChildren<TMP_Text>(true).text = "X";
        var layout = clone.GetComponent<LayoutElement>();
        if (layout != null) layout.ignoreLayout = true;

        var move = (RectTransform)src.parent;
        var rt = (RectTransform)clone.transform;
        rt.sizeDelta = new Vector2(34.1f, 27.3f);
        rt.anchoredPosition = new Vector2(move.sizeDelta.x / 2f - 31f, -22f);
    }
}
