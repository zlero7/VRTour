using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 [VRTour > 카페 메뉴판 꾸미기 (Scene1)] 한 번으로 Scene1(360도 식당 = 카페)에 주문 UI를 배치.
/// 다시 실행하면 기존 "CafeOrder" 오브젝트를 지우고 새로 만듦 (Ctrl+Z로 되돌리기 가능).
/// </summary>
public static class CafeBuilder
{
    private const string RootName = "CafeOrder";

    // 플레이어 시작 위치 (0, 1.36, -10.1)에서 앞으로 1.2m, 360도 구체(반지름 2.5m) 안쪽
    private static readonly Vector3 PlayerPos = new Vector3(0f, 0f, -10.1f);
    private static readonly Vector3 CanvasPos = new Vector3(0f, 1.2f, -8.9f);
    // 왼쪽 위는 머리에 붙은 이동 UI가 있어서 오른쪽에. 360도 구체 안쪽에 들어오도록 구체 중심에서 수평 2m 이내
    private static readonly Vector3 OwnerPos = new Vector3(1.25f, 0f, -8.2f);
    private const float CanvasScale = 0.0012f;

    private static readonly Color Ink = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color Brown = new Color(0.55f, 0.35f, 0.2f);

    [MenuItem("VRTour/카페 메뉴판 꾸미기 (Scene1)")]
    private static void BuildFromMenu() => Build(true);

    internal static void Build(bool ask)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (ask && scene.name != "Scene1" &&
            !EditorUtility.DisplayDialog("카페 메뉴판", $"지금 열린 씬은 '{scene.name}'입니다. 그래도 여기에 만들까요?", "만들기", "취소"))
            return;

        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        PharmacyBuilder.LoadUiAssets();

        var cafe = new GameObject(RootName).AddComponent<CafeOrder>();
        var audio = cafe.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PharmacyBuilder.SfxPath);
        cafe.sfx = audio;

        // ----- 카페 사장님 NPC (메뉴판 오른쪽, 플레이어를 바라봄) -----
        var npc = PharmacyBuilder.MakeStaff("Cafe_Owner", cafe.transform, OwnerPos, "NPC_Apron", Brown);
        Vector3 toPlayer = PlayerPos - OwnerPos;
        toPlayer.y = 0f;
        npc.rotation = Quaternion.LookRotation(-toPlayer); // NPC 얼굴은 -z 쪽
        cafe.staff = PharmacyBuilder.MakeTalkable(npc, "사장님께 말 걸기", OwnerPos + new Vector3(0f, 1.95f, 0f));
        Transform talkCanvas = cafe.staff.talkButton.transform.parent;
        talkCanvas.rotation = Quaternion.LookRotation(-toPlayer);

        var canvas = PharmacyBuilder.MakeCanvas("Cafe_Canvas", cafe.transform, CanvasPos, new Vector2(1000f, 700f), CanvasScale);

        // ----- 사장님 말풍선 (위) -----
        var bubble = PharmacyBuilder.MakePanel("Bubble", canvas, new Vector2(0f, 215f), new Vector2(1000f, 260f), Color.white);
        PharmacyBuilder.MakeUIText("Name", bubble, "카페 사장님", new Vector2(-360f, 100f), new Vector2(240f, 56f), 40f, Brown);
        cafe.speechText = PharmacyBuilder.MakeUIText("Speech", bubble, "", new Vector2(0f, -15f), new Vector2(920f, 200f), 54f, Ink);
        cafe.speechBubble = bubble.gameObject;

        // ----- 메뉴 고르기 (아래) -----
        var menu = PharmacyBuilder.MakePanel("Menu_Panel", canvas, new Vector2(0f, -150f), new Vector2(1000f, 380f), new Color(1f, 0.97f, 0.9f, 0.95f));
        PharmacyBuilder.MakeUIText("Title", menu, "오늘의 메뉴", new Vector2(0f, 120f), new Vector2(900f, 80f), 56f, Brown);
        cafe.menuPanel = menu.gameObject;
        cafe.menuButtons = new[]
        {
            PharmacyBuilder.MakeButton("Menu_0", menu, "", new Vector2(-315f, -50f), new Vector2(290f, 160f), new Color(0.98f, 0.8f, 0.45f)),
            PharmacyBuilder.MakeButton("Menu_1", menu, "", new Vector2(0f, -50f), new Vector2(290f, 160f), new Color(0.8f, 0.68f, 0.55f)),
            PharmacyBuilder.MakeButton("Menu_2", menu, "", new Vector2(315f, -50f), new Vector2(290f, 160f), new Color(0.95f, 0.55f, 0.5f)),
        };

        // ----- 주문 완료 -----
        var done = PharmacyBuilder.MakePanel("Done_Panel", canvas, new Vector2(0f, -150f), new Vector2(1000f, 380f), new Color(1f, 0.97f, 0.9f, 0.95f));
        cafe.donePanel = done.gameObject;
        cafe.doneText = PharmacyBuilder.MakeUIText("Done", done, "", new Vector2(0f, 70f), new Vector2(940f, 200f), 50f, Brown);
        cafe.pharmacyButton = PharmacyBuilder.MakeButton("Pharmacy", done, "약국 가기", new Vector2(-220f, -110f), new Vector2(360f, 110f), new Color(0.55f, 0.85f, 0.6f));
        cafe.reorderButton = PharmacyBuilder.MakeButton("Reorder", done, "다시 고르기", new Vector2(220f, -110f), new Vector2(360f, 110f), new Color(0.98f, 0.8f, 0.45f));
        done.gameObject.SetActive(false);

        Undo.RegisterCreatedObjectUndo(cafe.gameObject, "카페 메뉴판 꾸미기");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = cafe.gameObject;
        Debug.Log("[CafeBuilder] 카페 메뉴판 배치 완료. 확인 후 Ctrl+S로 씬을 저장하세요.");
    }
}
