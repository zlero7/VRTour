using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 특화 기능 3개를 씬에 배치:
///  1) VR 사진 촬영 - 모든 씬 (PhotoCapture + 번쩍임 + 폴라로이드 카드)
///  2) 시선 응시 정보 포인트 - Scene1 (카페 360°)
///  3) 목적지 안내 화살표 - Scene2 (먹자골목) + 상단 [길안내] 버튼
/// SettingsBuilder의 [타이틀 + 설정 전체 씬에 적용]에서 같이 실행되고, 다시 실행하면 기존 것을 지우고 새로 만듦.
/// </summary>
public static class FeatureBuilder
{
    private const string GuideFolder = "Assets/04_Resource/Guide";

    private static readonly Color Ink = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color Brown = new Color(0.55f, 0.35f, 0.2f);
    private static readonly Color Cream = new Color(1f, 0.97f, 0.9f, 0.95f);
    private static readonly Color Dark = new Color(0f, 0f, 0f, 0.75f);

    internal static void Build(Scene scene)
    {
        PharmacyBuilder.LoadUiAssets();
        BuildPhoto(scene);
        if (scene.name == "Scene1") BuildGaze(scene);
        if (scene.name == "Scene2") BuildGuide(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void Remove(Scene scene, params string[] names)
    {
        foreach (var name in names)
        {
            var old = SettingsBuilder.Find(scene, name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
    }

    private static AudioClip ClickClip() => AssetDatabase.LoadAssetAtPath<AudioClip>(PharmacyBuilder.SfxPath);

    // ===================== 1. VR 사진 촬영 =====================

    private static void BuildPhoto(Scene scene)
    {
        Remove(scene, "Photo_Capture", "Photo_Flash", "Photo_Card");

        var cam = SettingsBuilder.Find(scene, "Main Camera");
        var moveUI = SettingsBuilder.Find(scene, "MoveUI");
        if (cam == null || moveUI == null) return;
        float scale = moveUI.lossyScale.x;

        var root = new GameObject("Photo_Capture");
        var capture = root.AddComponent<PhotoCapture>();
        var audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        capture.sfx = audio;

        // 번쩍임: 눈앞 0.4m에 시야를 다 덮는 흰 판 (평소엔 투명, 레이 입력은 막지 않음)
        var flashCanvas = PharmacyBuilder.MakeCanvas("Photo_Flash", cam, cam.TransformPoint(new Vector3(0f, 0f, 0.4f)), new Vector2(800f, 800f), scale);
        var flashPanel = PharmacyBuilder.MakePanel("Flash", flashCanvas, Vector2.zero, new Vector2(800f, 800f), Color.white);
        flashPanel.GetComponent<Image>().raycastTarget = false;
        capture.flash = flashCanvas.gameObject.AddComponent<CanvasGroup>();
        capture.flash.blocksRaycasts = false;
        capture.flash.interactable = false;

        // 폴라로이드 카드: 시야 오른쪽 아래 (너무 크지 않게 조금 멀리, 작은 비율로)
        var cardCanvas = PharmacyBuilder.MakeCanvas("Photo_Card", cam, cam.TransformPoint(new Vector3(0.3f, -0.2f, 1f)), new Vector2(280f, 230f), scale * 0.8f);
        var card = PharmacyBuilder.MakePanel("Card", cardCanvas, Vector2.zero, new Vector2(280f, 230f), Color.white);
        var photoGo = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
        var photoRt = (RectTransform)photoGo.transform;
        photoRt.SetParent(card, false);
        photoRt.anchoredPosition = new Vector2(0f, 24f);
        photoRt.sizeDelta = new Vector2(256f, 144f);
        capture.cardImage = photoGo.GetComponent<RawImage>();
        capture.cardImage.raycastTarget = false;
        capture.cardCaption = PharmacyBuilder.MakeUIText("Caption", card, "", new Vector2(0f, -82f), new Vector2(260f, 40f), 22f, Ink);
        capture.card = card.gameObject;

        EditorUtility.SetDirty(capture);
    }

    // ===================== 2. 시선 응시 정보 포인트 (Scene1) =====================

    private struct HotspotSpec
    {
        public string name, title, body;
        public float vx, vy; // 게임 화면(시작 시점)에서 가리킬 곳의 위치 (0~1)
    }

    private static readonly HotspotSpec[] Hotspots =
    {
        new HotspotSpec { name = "Hotspot_Window", vx = 0.86f, vy = 0.44f, title = "창가 꽃 그림",
            body = "창문처럼 보이는 꽃 풍경 그림이에요.\n벽에 바깥 풍경이 열린 듯한 느낌을 줘요." },
        new HotspotSpec { name = "Hotspot_Lamp", vx = 0.304f, vy = 0.59f, title = "동그란 전구 조명",
            body = "천장에 매달린 둥근 전구가\n카페 안을 은은하게 밝혀 줘요." },
        new HotspotSpec { name = "Hotspot_Ceiling", vx = 0.674f, vy = 0.888f, title = "손님들의 낙서",
            body = "천장과 벽에 다녀간 손님들이 남긴\n글씨가 가득해요. 이 카페만의 추억이에요." },
    };

    private static void BuildGaze(Scene scene)
    {
        Remove(scene, "Gaze_Hotspots");
        var cam = SettingsBuilder.Find(scene, "Main Camera");
        if (cam == null) return;

        var camera = cam.GetComponent<Camera>();
        const float aspect = 905f / 403f; // 위치를 잡을 때 본 게임 화면 비율
        float th = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // 360° 사진은 구체 안쪽 면에 붙어 있고 카메라는 구체 중심에서 조금 벗어나 있어서,
        // 카메라에서 본 방향의 구체 표면 위에 마커를 둬야 사진 속 물건과 겹침
        var sphere = SettingsBuilder.Find(scene, "Sphere");

        var root = new GameObject("Gaze_Hotspots");
        foreach (var spec in Hotspots)
        {
            // 화면 위치 → 카메라 기준 방향 → 360° 구체 안쪽 1.9m 지점
            float yaw = Mathf.Atan((spec.vx - 0.5f) * 2f * th * aspect) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan((spec.vy - 0.5f) * 2f * th) * Mathf.Rad2Deg;
            Vector3 dir = cam.rotation * Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
            Vector3 pos = OnSphere(cam.position, dir, sphere);

            var hs = new GameObject(spec.name).AddComponent<GazeHotspot>();
            hs.transform.SetParent(root.transform, false);
            hs.transform.position = pos;
            hs.title = spec.title;
            hs.description = spec.body;

            var canvas = PharmacyBuilder.MakeCanvas("Marker_Canvas", hs.transform, pos, new Vector2(460f, 330f), 0.0012f);
            hs.canvasRoot = canvas;

            var ring = MakeKnob("Ring", canvas, knob, new Color(1f, 1f, 1f, 0.6f));
            hs.ring = ring;
            var gauge = MakeKnob("Gauge", canvas, knob, new Color(0.45f, 0.85f, 0.55f, 0.95f));
            var gaugeImage = gauge.GetComponent<Image>();
            gaugeImage.type = Image.Type.Filled;
            gaugeImage.fillMethod = Image.FillMethod.Radial360;
            gaugeImage.fillOrigin = (int)Image.Origin360.Top;
            gaugeImage.fillClockwise = true;
            gaugeImage.fillAmount = 0f;
            hs.gauge = gaugeImage;

            // 설명 카드는 마커와 다른 캔버스: 구체 표면에 두면 가장자리가 구체 밖으로 나가 잘려서, 실행 중에 카메라 쪽으로 당겨 놓음
            var cardCanvas = PharmacyBuilder.MakeCanvas("Card_Canvas", hs.transform, pos, new Vector2(460f, 210f), 0.0012f * hs.cardDistanceRatio * 1.6f);
            hs.cardRoot = cardCanvas;
            var card = PharmacyBuilder.MakePanel("Card", cardCanvas, Vector2.zero, new Vector2(460f, 210f), Cream);
            hs.titleText = PharmacyBuilder.MakeUIText("Title", card, "", new Vector2(0f, 65f), new Vector2(420f, 60f), 44f, Brown);
            hs.bodyText = PharmacyBuilder.MakeUIText("Body", card, "", new Vector2(0f, -35f), new Vector2(430f, 120f), 32f, Ink);
            hs.card = card.gameObject;

            EditorUtility.SetDirty(hs);
        }
    }

    // 카메라에서 dir 방향으로 쏜 선이 구체 안쪽 면에 닿는 지점보다 3% 안쪽
    private static Vector3 OnSphere(Vector3 origin, Vector3 dir, Transform sphere)
    {
        if (sphere == null) return origin + dir * 1.9f;

        Vector3 center = sphere.position;
        float radius = sphere.lossyScale.x * 0.5f;
        Vector3 oc = origin - center;
        float b = Vector3.Dot(oc, dir);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float t = -b + Mathf.Sqrt(Mathf.Max(0f, b * b - c)); // 안쪽에서 나가는 쪽 교점
        return origin + dir * (t * 0.97f);
    }

    private static RectTransform MakeKnob(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(110f, 110f);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    // ===================== 3. 목적지 안내 화살표 (Scene2) =====================

    private static void BuildGuide(Scene scene)
    {
        Remove(scene, "Guide_Arrow", "Guide_Label", "Guide_Canvas", "GuideBtn");

        var cam = SettingsBuilder.Find(scene, "Main Camera");
        var moveUI = SettingsBuilder.Find(scene, "MoveUI");
        // 이름이 같은 맵 이동 버튼(MapTP 아래)이 따로 있어서, 실제 이동 지점인 씬 최상위 오브젝트를 찾음
        var cafe = FindRoot(scene, "CoffeePoint");
        var pharmacy = FindRoot(scene, "PharmacyPoint");
        if (cam == null || moveUI == null || cafe == null || pharmacy == null)
        {
            Debug.LogError("[FeatureBuilder] Scene2에서 Main Camera/MoveUI/CoffeePoint/PharmacyPoint를 찾지 못했습니다.");
            return;
        }

        // ----- 화살표 본체 -----
        var material = GetOrCreateArrowMaterial();
        var headMesh = GetOrCreateHeadMesh();

        var guide = new GameObject("Guide_Arrow").AddComponent<GuideArrow>();
        var audio = guide.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = ClickClip();
        guide.sfx = audio;

        var arrow = new GameObject("Arrow").transform;
        arrow.SetParent(guide.transform, false);
        guide.arrow = arrow;

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "Shaft";
        Object.DestroyImmediate(shaft.GetComponent<Collider>());
        shaft.transform.SetParent(arrow, false);
        shaft.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        shaft.transform.localScale = new Vector3(0.07f, 0.025f, 0.28f);
        shaft.GetComponent<MeshRenderer>().sharedMaterial = material;

        var head = new GameObject("Head", typeof(MeshFilter), typeof(MeshRenderer));
        head.transform.SetParent(arrow, false);
        head.transform.localPosition = new Vector3(0f, 0f, 0.04f);
        head.GetComponent<MeshFilter>().sharedMesh = headMesh;
        head.GetComponent<MeshRenderer>().sharedMaterial = material;

        // ----- 남은 거리 글자판 -----
        var labelCanvas = PharmacyBuilder.MakeCanvas("Guide_Label", null, Vector3.zero, new Vector2(420f, 100f), 0.0015f);
        SceneManager.MoveGameObjectToScene(labelCanvas.gameObject, scene);
        var labelPanel = PharmacyBuilder.MakePanel("Panel", labelCanvas, Vector2.zero, new Vector2(420f, 100f), new Color(0f, 0f, 0f, 0.7f));
        guide.label = PharmacyBuilder.MakeUIText("Label", labelPanel, "", Vector2.zero, new Vector2(400f, 85f), 44f, Color.white);
        guide.labelRoot = labelCanvas;

        guide.targets = new[]
        {
            new GuideArrow.Target { name = "카페", point = cafe },
            new GuideArrow.Target { name = "약국", point = pharmacy },
        };

        // ----- 길안내 메뉴 (머리에 붙음, 설정 패널과 같은 자리) -----
        var canvas = PharmacyBuilder.MakeCanvas("Guide_Canvas", cam, moveUI.position, new Vector2(360f, 320f), moveUI.lossyScale.x);
        var menu = canvas.gameObject.AddComponent<GuideMenu>();
        menu.arrow = guide;
        var panel = PharmacyBuilder.MakePanel("Panel", canvas, Vector2.zero, new Vector2(360f, 320f), Dark);
        menu.panel = panel.gameObject;

        MakeLabel(panel, "Title", "길안내", new Vector2(0f, 125f), new Vector2(200f, 40f), 30f);
        menu.closeButton = MakeButton(panel, "CloseBtn", "X", new Vector2(150f, 125f), new Vector2(44f, 36f), new Color(0.95f, 0.55f, 0.5f));
        menu.cafeButton = MakeButton(panel, "CafeBtn", "카페로 안내", new Vector2(0f, 55f), new Vector2(320f, 56f), new Color(1f, 0.9f, 0.45f));
        menu.pharmacyButton = MakeButton(panel, "PharmacyBtn", "약국으로 안내", new Vector2(0f, -17f), new Vector2(320f, 56f), new Color(0.55f, 0.85f, 0.6f));
        menu.offButton = MakeButton(panel, "OffBtn", "안내 끄기", new Vector2(0f, -89f), new Vector2(320f, 56f), new Color(0.85f, 0.85f, 0.9f));
        MakeLabel(panel, "Hint", "화살표를 따라 걸어가 보세요", new Vector2(0f, -140f), new Vector2(330f, 26f), 15f);

        // 같은 자리에 뜨는 다른 메뉴와 겹치지 않게 서로 숨기고, 설정 패널도 이 메뉴가 열리면 닫히게 함
        var settings = Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        var hide = new List<GameObject> { moveUI.gameObject };
        var mapTp = SettingsBuilder.Find(scene, "MapTPUI");
        if (mapTp != null) hide.Add(mapTp.gameObject);
        if (settings != null)
        {
            hide.Add(settings.panel);
            var list = new List<GameObject>(settings.hideWhileOpen) { menu.panel };
            settings.hideWhileOpen = list.ToArray();
            EditorUtility.SetDirty(settings);
        }
        menu.hideWhileOpen = hide.ToArray();

        // ----- 상단 바에 [길안내] 버튼 -----
        var button = SettingsBuilder.AddBarButton(scene, "GuideBtn", "길안내");
        if (button != null)
        {
            UnityEventTools.AddPersistentListener(button.onClick, menu.Toggle);
            var sfx = button.GetComponent<SfxPlayer>();
            if (sfx != null) UnityEventTools.AddPersistentListener(button.onClick, sfx.Play);
            SettingsBuilder.LayoutBar(scene);
        }

        EditorUtility.SetDirty(guide);
        EditorUtility.SetDirty(menu);
    }

    private static Transform FindRoot(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) return root.transform;
        return null;
    }

    private static TextMeshProUGUI MakeLabel(RectTransform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize)
        => PharmacyBuilder.MakeUIText(name, parent, text, pos, size, fontSize, Color.white);

    private static Button MakeButton(RectTransform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        var button = PharmacyBuilder.MakeButton(name, parent, label, pos, size, color);
        var tmp = button.GetComponentInChildren<TMP_Text>();
        tmp.fontSizeMax = 22f;
        tmp.fontSizeMin = 14f;
        tmp.fontSize = 22f;
        return button;
    }

    private static Material GetOrCreateArrowMaterial()
    {
        EnsureFolder();
        string path = GuideFolder + "/GuideArrow.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", new Color(1f, 0.75f, 0.15f));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // 화살촉: 앞이 +Z인 납작한 삼각기둥 (밑변 폭 0.24, 길이 0.22, 두께 0.025)
    private static Mesh GetOrCreateHeadMesh()
    {
        EnsureFolder();
        string path = GuideFolder + "/ArrowHead.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;

        const float t = 0.0125f;
        var mesh = new Mesh { name = "ArrowHead" };
        mesh.vertices = new[]
        {
            new Vector3(-0.12f, t, 0f),  // 0 At
            new Vector3(0.12f, t, 0f),   // 1 Bt
            new Vector3(0f, t, 0.22f),   // 2 Ct
            new Vector3(-0.12f, -t, 0f), // 3 Ab
            new Vector3(0.12f, -t, 0f),  // 4 Bb
            new Vector3(0f, -t, 0.22f),  // 5 Cb
        };
        mesh.triangles = new[]
        {
            0, 2, 1,           // 위
            3, 4, 5,           // 아래
            0, 1, 4, 0, 4, 3,  // 뒤
            1, 2, 5, 1, 5, 4,  // 오른쪽 옆
            2, 0, 3, 2, 3, 5,  // 왼쪽 옆
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(GuideFolder)) AssetDatabase.CreateFolder("Assets/04_Resource", "Guide");
    }
}
