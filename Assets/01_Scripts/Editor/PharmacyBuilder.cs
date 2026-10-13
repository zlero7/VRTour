using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 메뉴 [VRTour > 약국 꾸미기 (Scene3)] 한 번으로 Scene3에 약국 실내와 체험(약사, 증상 선택, 약 찾기)을 배치.
/// 다시 실행하면 기존 "Pharmacy" 오브젝트를 지우고 새로 만듦 (Ctrl+Z로 되돌리기 가능).
/// </summary>
public static class PharmacyBuilder
{
    private const string RootName = "Pharmacy";
    private const string MatDir = "Assets/04_Resource/Pharmacy";
    private const string FurnitureDir = "Assets/Low Poly Furniture/Prefabs/";
    private const string CoffeeDir = "Assets/Stylized Low Poly Coffee Shop Props Pack – Free/Prefabs/";
    private const string CuteDir = "Assets/ithappy/Cute_Furniture_Free/Prefabs/";
    private const string JarDir = "Assets/First aid jar/Prefabs/";
    private const string FontPath = "Assets/03_Font/Stellar_Serif_Bold_Condensed SDF.asset";
    internal const string SfxPath = "Assets/05_Audio/마우스 클릭 효과음 - 사운드 캣 (320k).mp3";

    // 방 크기 (Scene3 바닥 Plane 위, 플레이어 시작 위치 (0, 0, -10) 기준)
    private const float XLeft = -4f, XRight = 4f, ZFront = -12f, ZBack = -3f;
    private const float WallHeight = 3f, WallThick = 0.1f, WallGap = 0.02f;
    private const float ItemShelfHeight = 1.1f; // 진열대에 놓는 약의 높이 (손 닿기 좋은 높이)

    // 외부(작업 도우미)에서 이 파일을 만들면 에디터가 자동으로 약국을 배치하고 결과 파일을 남김
    private const string RequestFile = "Temp/VRTour_BuildPharmacy.request";
    private const string ResultFile = "Temp/VRTour_BuildPharmacy.result";
    private static readonly List<string> report = new List<string>();
    private static double nextCheck;

    private static TMP_FontAsset font;
    private static Sprite roundSprite;

    [InitializeOnLoadMethod]
    private static void WatchRequest()
    {
        EditorApplication.update += () =>
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1.0;
            if (!System.IO.File.Exists(RequestFile)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            System.IO.File.Delete(RequestFile);
            try
            {
                if (EditorSceneManager.GetActiveScene().name != "Scene3")
                    EditorSceneManager.OpenScene("Assets/00_Scenes/Scene3.unity");
                Build(false);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                report.Add("SAVED");
            }
            catch (System.Exception e)
            {
                report.Add("EXCEPTION: " + e);
            }
            System.IO.File.WriteAllLines(ResultFile, report);
        };
    }

    [MenuItem("VRTour/약국 꾸미기 (Scene3)")]
    private static void BuildFromMenu() => Build(true);

    private static void Build(bool ask)
    {
        report.Clear();
        var scene = EditorSceneManager.GetActiveScene();
        if (ask && scene.name != "Scene3" &&
            !EditorUtility.DisplayDialog("약국 꾸미기", $"지금 열린 씬은 '{scene.name}'입니다. 그래도 여기에 만들까요?", "만들기", "취소"))
            return;

        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject(RootName).transform;
        LoadUiAssets();

        Material wallMat = GetOrCreateMat("Wall", new Color(0.93f, 0.96f, 0.94f));
        Material signMat = GetOrCreateMat("SignPlate", new Color(0.15f, 0.6f, 0.4f));
        Material[] boxMats =
        {
            GetOrCreateMat("Box_White", new Color(0.95f, 0.95f, 0.95f)),
            GetOrCreateMat("Box_Blue", new Color(0.55f, 0.75f, 0.95f)),
            GetOrCreateMat("Box_Pink", new Color(0.95f, 0.6f, 0.7f)),
            GetOrCreateMat("Box_Yellow", new Color(0.98f, 0.85f, 0.4f)),
            GetOrCreateMat("Box_Green", new Color(0.55f, 0.85f, 0.6f)),
            GetOrCreateMat("Box_Orange", new Color(0.98f, 0.65f, 0.35f)),
        };
        var rng = new System.Random(7);

        // ----- 벽 -----
        var walls = Group("Walls", root);
        float midX = (XLeft + XRight) / 2f, midZ = (ZFront + ZBack) / 2f;
        float width = XRight - XLeft + WallThick * 2f, depth = ZBack - ZFront;
        MakeCube("Wall_Back", walls, new Vector3(midX, WallHeight / 2f, ZBack + WallThick / 2f), new Vector3(width, WallHeight, WallThick), wallMat, true);
        MakeCube("Wall_Front", walls, new Vector3(midX, WallHeight / 2f, ZFront - WallThick / 2f), new Vector3(width, WallHeight, WallThick), wallMat, true);
        MakeCube("Wall_Left", walls, new Vector3(XLeft - WallThick / 2f, WallHeight / 2f, midZ), new Vector3(WallThick, WallHeight, depth), wallMat, true);
        MakeCube("Wall_Right", walls, new Vector3(XRight + WallThick / 2f, WallHeight / 2f, midZ), new Vector3(WallThick, WallHeight, depth), wallMat, true);

        // ----- 진열대 (뒷벽 3개, 양옆 벽 2개씩) + 약 상자 채우기 -----
        var shelves = Group("Shelves", root);
        string shelfPath = FurnitureDir + "Bookshelf/Standart Bookshelf.prefab";
        foreach (float x in new[] { -2.6f, 0f, 2.6f })
            PlaceShelf(shelfPath, shelves, new Vector3(x, 0f, ZBack - 0.5f), 0f, Vector3.forward, ZBack, boxMats, rng, false);
        Vector3? rightSpot = null, leftSpot = null;
        foreach (float z in new[] { -6f, -8.6f })
        {
            bool reserve = z > -7f; // 계산대 쪽 진열대에 찾을 약을 놓음
            var l = PlaceShelf(shelfPath, shelves, new Vector3(XLeft + 0.5f, 0f, z), 90f, Vector3.left, XLeft, boxMats, rng, reserve);
            var r = PlaceShelf(shelfPath, shelves, new Vector3(XRight - 0.5f, 0f, z), 90f, Vector3.right, XRight, boxMats, rng, reserve);
            if (reserve) { leftSpot = l; rightSpot = r; }
        }

        // ----- 체험 진행 오브젝트 -----
        var game = new GameObject("PharmacyGame").AddComponent<PharmacyGame>();
        game.transform.SetParent(root, false);
        var audio = game.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxPath);
        game.sfx = audio;

        // ----- 계산대 -----
        var counter = Place(CoffeeDir + "Counter_1.prefab", root, new Vector3(0f, 0f, -5f), 0f, 1.0f);
        Bounds cb = counter != null ? GetBounds(counter) : new Bounds(new Vector3(0f, 0.5f, -5f), new Vector3(2f, 1f, 1f));
        if (counter != null)
        {
            FaceOpenSide(counter, Vector3.forward); // 직원 쪽(열린 면)이 뒷벽을 보도록
            cb = GetBounds(counter);
            AddStaticCollider("Counter_Collider", root, cb);
            Place(CoffeeDir + "Cash_Register.prefab", counter.transform, new Vector3(cb.center.x + cb.size.x * 0.25f, cb.max.y, cb.center.z), 180f, 0.3f);
        }

        var trigger = new GameObject("Checkout_Trigger");
        trigger.transform.SetParent(root, false);
        trigger.transform.position = new Vector3(cb.center.x, cb.max.y + 0.25f, cb.center.z);
        var tc = trigger.AddComponent<BoxCollider>();
        tc.isTrigger = true;
        tc.size = new Vector3(cb.size.x, 0.5f, cb.size.z);
        trigger.AddComponent<PharmacyCheckout>().game = game;

        // ----- 약사 NPC + 말풍선 -----
        Vector3 npcPos = new Vector3(cb.center.x - 0.6f, 0f, Mathf.Min(cb.max.z + 0.45f, ZBack - 0.75f));
        game.npc = MakePharmacist(root, npcPos);
        // 약사 오른쪽 어깨 옆 (말풍선, 증상 패널과 겹치지 않는 자리)
        game.staff = MakeTalkable(game.npc, "약사에게 말 걸기", npcPos + new Vector3(0.5f, 1.45f, -0.3f));

        var speechCanvas = MakeCanvas("Speech_Canvas", root, new Vector3(npcPos.x, 2.02f, cb.max.z), new Vector2(1000f, 360f), 0.0013f);
        MakePanel("Bubble", speechCanvas, Vector2.zero, new Vector2(1000f, 360f), Color.white);
        MakeUIText("Name", speechCanvas, "약사", new Vector2(-400f, 140f), new Vector2(160f, 60f), 40f, new Color(0.15f, 0.6f, 0.4f));
        game.speechText = MakeUIText("Speech", speechCanvas, "", new Vector2(0f, -15f), new Vector2(920f, 290f), 58f, new Color(0.15f, 0.15f, 0.15f));
        game.speechBubble = speechCanvas.gameObject;

        // ----- 증상 선택 / 결과 패널 (계산대 앞) -----
        // 약사 얼굴을 가리지 않도록 오른쪽으로 비켜서 배치
        var uiCanvas = MakeCanvas("Interaction_Canvas", root, new Vector3(cb.center.x + 0.75f, cb.max.y + 0.15f, cb.min.z - 0.35f), new Vector2(1000f, 380f), 0.0013f);

        var choice = MakePanel("Choice_Panel", uiCanvas, Vector2.zero, new Vector2(1000f, 380f), new Color(1f, 1f, 1f, 0.92f));
        MakeUIText("Title", choice, "어디가 불편하세요?", new Vector2(0f, 120f), new Vector2(900f, 80f), 56f, new Color(0.15f, 0.15f, 0.15f));
        game.choicePanel = choice.gameObject;
        game.choiceButtons = new[]
        {
            MakeButton("Choice_0", choice, "", new Vector2(-315f, -50f), new Vector2(290f, 160f), new Color(0.98f, 0.85f, 0.4f)),
            MakeButton("Choice_1", choice, "", new Vector2(0f, -50f), new Vector2(290f, 160f), new Color(0.55f, 0.75f, 0.95f)),
            MakeButton("Choice_2", choice, "", new Vector2(315f, -50f), new Vector2(290f, 160f), new Color(0.95f, 0.6f, 0.7f)),
        };

        var result = MakePanel("Result_Panel", uiCanvas, Vector2.zero, new Vector2(1000f, 380f), new Color(1f, 1f, 1f, 0.95f));
        game.resultPanel = result.gameObject;
        game.resultText = MakeUIText("Result", result, "", new Vector2(0f, 70f), new Vector2(940f, 220f), 52f, new Color(0.1f, 0.45f, 0.3f));
        game.homeButton = MakeButton("Home", result, "처음으로", new Vector2(-220f, -120f), new Vector2(360f, 110f), new Color(0.55f, 0.85f, 0.6f));
        game.retryButton = MakeButton("Retry", result, "다시 하기", new Vector2(220f, -120f), new Vector2(360f, 110f), new Color(0.55f, 0.75f, 0.95f));

        game.confetti = MakeConfetti(root, new Vector3(cb.center.x, cb.max.y + 0.2f, cb.center.z));

        // ----- 약 진열 테이블 + 찾을 약들 -----
        var items = Group("Medicines", root);
        var table = Place(FurnitureDir + "Table/Round Table.prefab", root, new Vector3(0f, 0f, -7.8f), 0f, 0.75f);
        if (table != null)
        {
            Bounds tb = GetBounds(table);
            AddStaticCollider("Table_Collider", root, tb);
            float top = tb.max.y + 0.01f;
            MakeJar(items, "두통약", JarDir + "Firstaid_2.prefab", new Vector3(tb.center.x - 0.35f, top, tb.center.z));
            MakeBox(items, "파스", new Vector3(tb.center.x, top, tb.center.z - 0.1f), new Vector3(0.16f, 0.04f, 0.11f), boxMats[0]);
            MakeBox(items, "감기약", new Vector3(tb.center.x + 0.35f, top, tb.center.z), new Vector3(0.09f, 0.13f, 0.06f), boxMats[2]);
            game.hintText = MakeText("Hint_Text", root, "약사에게 증상을 말하고\n필요한 약을 찾아보세요", new Vector3(tb.center.x, tb.max.y + 1.7f, tb.center.z), 1.6f, new Color(0.2f, 0.2f, 0.2f)); // 시작 위치에서 약사와 말풍선을 가리지 않게 시선보다 위에
        }
        if (rightSpot.HasValue) MakeJar(items, "소화제", JarDir + "Firstaid.prefab", rightSpot.Value);
        if (leftSpot.HasValue) MakeBox(items, "목캔디", leftSpot.Value, new Vector3(0.1f, 0.06f, 0.06f), boxMats[3]);

        // ----- 간판 -----
        MakeCube("Sign_Plate", root, new Vector3(0f, 2.6f, ZBack - 0.03f), new Vector3(2.4f, 0.55f, 0.04f), signMat, false);
        MakeText("Sign_Text", root, "불광 약국", new Vector3(0f, 2.6f, ZBack - 0.06f), 4f, Color.white);

        // ----- 장식 화분 -----
        Place(FurnitureDir + "Flower/Green Plant.prefab", root, new Vector3(XLeft + 0.6f, 0f, ZFront + 0.6f), 0f, 1.1f);
        Place(FurnitureDir + "Flower/Green Plant.prefab", root, new Vector3(XRight - 0.6f, 0f, ZFront + 0.6f), 0f, 1.1f);
        Place(CuteDir + "Plants/Plants_15.prefab", root, new Vector3(XLeft + 0.4f, 0f, ZBack - 1.6f), 0f, 0.9f);
        Place(CuteDir + "Plants/Plants_15.prefab", root, new Vector3(XRight - 0.4f, 0f, ZBack - 1.6f), 0f, 0.9f);

        int boxCount = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "MedicineBox") { boxCount++; continue; }
            if (child.parent != root && (child.parent == null || child.parent.parent != root)) continue;
            Bounds bx = GetBounds(child.gameObject);
            report.Add($"OBJ {child.name}: center={bx.center:F2} size={bx.size:F2} rotY={child.eulerAngles.y:F0}");
        }
        report.Add("MEDICINE BOXES: " + boxCount);
        report.Add($"SPOTS right={rightSpot} left={leftSpot}");

        Undo.RegisterCreatedObjectUndo(root.gameObject, "약국 꾸미기");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("[PharmacyBuilder] 약국 배치 완료. 확인 후 Ctrl+S로 씬을 저장하세요.");
    }

    // ===================== 가구 =====================

    // 진열대 배치: 열린 면이 방 안쪽을 보게 돌리고, 벽에 붙인 뒤 칸마다 약 상자 채우기.
    // reserve면 가운데 칸 하나(손 닿는 높이)를 비워두고 그 위치를 돌려줌
    private static Vector3? PlaceShelf(string path, Transform parent, Vector3 pos, float yRot, Vector3 towardWall, float wallCoord,
        Material[] mats, System.Random rng, bool reserve)
    {
        var shelf = Place(path, parent, pos, yRot, 2.0f);
        if (shelf == null) return null;
        FaceOpenSide(shelf, -towardWall);
        SnapToWall(shelf, towardWall, wallCoord);

        var colliders = AddMeshColliders(shelf); // 선반 위에 약을 올려둘 수 있게 충돌체 유지
        Bounds b = GetBounds(shelf);

        Vector3? spot = null;
        if (reserve)
        {
            float best = float.MaxValue;
            foreach (var s in SurfacesBelow(new Vector3(b.center.x, b.max.y + 0.5f, b.center.z), b.size.y + 1f, b.max.y, colliders))
            {
                float diff = Mathf.Abs(s.point.y - ItemShelfHeight);
                if (diff < best) { best = diff; spot = s.point; }
            }
        }

        Stock(shelf, b, colliders, mats, rng, spot);
        return spot;
    }

    // 위에서 아래로 레이를 쏴서 선반 칸 바닥을 찾고, 칸 높이에 맞는 약 상자를 채움
    private static void Stock(GameObject shelf, Bounds b, List<MeshCollider> colliders, Material[] mats, System.Random rng, Vector3? keepEmpty)
    {
        bool alongX = b.size.x >= b.size.z;
        float length = alongX ? b.size.x : b.size.z;
        float shelfDepth = alongX ? b.size.z : b.size.x;
        int count = Mathf.Max(2, Mathf.FloorToInt((length - 0.15f) / 0.16f));

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count - 0.5f;
            Vector3 origin = b.center + (alongX ? Vector3.right : Vector3.forward) * (t * (length - 0.15f));
            origin.y = b.max.y + 0.5f;

            foreach (var s in SurfacesBelow(origin, b.size.y + 1f, b.max.y, colliders))
            {
                if (s.clearance < 0.08f) continue;
                if (keepEmpty.HasValue && Mathf.Abs(s.point.y - keepEmpty.Value.y) < 0.02f &&
                    Vector2.Distance(new Vector2(s.point.x, s.point.z), new Vector2(keepEmpty.Value.x, keepEmpty.Value.z)) < 0.2f)
                    continue;

                float h = Mathf.Min(Lerp(rng, 0.1f, 0.2f), s.clearance * 0.85f);
                float w = Lerp(rng, 0.08f, 0.13f);
                float d = Mathf.Min(Lerp(rng, 0.06f, 0.1f), shelfDepth * 0.7f);
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "MedicineBox";
                Object.DestroyImmediate(box.GetComponent<Collider>());
                box.GetComponent<Renderer>().sharedMaterial = mats[rng.Next(mats.Length)];
                box.transform.position = s.point + Vector3.up * (h / 2f);
                box.transform.localScale = alongX ? new Vector3(w, h, d) : new Vector3(d, h, w);
                box.transform.SetParent(shelf.transform, true);
            }
        }
    }

    // origin에서 아래로 내려가며 위를 향한 면(선반 칸 바닥)을 모두 찾음. 맨 위 지붕은 제외
    private static List<(Vector3 point, float clearance)> SurfacesBelow(Vector3 origin, float maxDown, float topY, List<MeshCollider> colliders)
    {
        var list = new List<(Vector3, float)>();
        float remaining = maxDown;
        for (int k = 0; k < 30 && remaining > 0f; k++)
        {
            if (!NearestHit(origin, Vector3.down, remaining, colliders, out RaycastHit hit)) break;
            if (hit.normal.y > 0.9f && hit.point.y < topY - 0.03f)
            {
                float clearance = 0.25f;
                if (NearestHit(hit.point + Vector3.up * 0.005f, Vector3.up, 3f, colliders, out RaycastHit up))
                    clearance = Mathf.Min(clearance, up.distance);
                list.Add((hit.point, clearance));
            }
            float step = hit.distance + 0.01f;
            origin += Vector3.down * step;
            remaining -= step;
        }
        return list;
    }

    // RaycastAll은 충돌체 하나당 한 번만 맞으므로, 가장 가까운 것만 골라 반복해서 사용
    private static bool NearestHit(Vector3 origin, Vector3 dir, float dist, List<MeshCollider> colliders, out RaycastHit best)
    {
        best = default;
        bool found = false;
        foreach (var hit in Physics.RaycastAll(origin, dir, dist))
        {
            if (!(hit.collider is MeshCollider mc) || !colliders.Contains(mc)) continue;
            if (!found || hit.distance < best.distance) { best = hit; found = true; }
        }
        return found;
    }

    private static GameObject Place(string path, Transform parent, Vector3 pos, float yRot, float height)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[PharmacyBuilder] 프리팹을 찾을 수 없음: {path}");
            report.Add("MISSING PREFAB: " + path);
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yRot, 0f));

        Bounds b = GetBounds(go);
        if (height > 0f && b.size.y > 0.0001f) go.transform.localScale *= height / b.size.y;

        // 바닥(pos.y)에 올려놓고 x, z 중심을 pos에 맞춤
        b = GetBounds(go);
        go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
        return go;
    }

    private static void SnapToWall(GameObject go, Vector3 towardWall, float wallCoord)
    {
        Bounds b = GetBounds(go);
        Vector3 d = Vector3.zero;
        if (towardWall.z > 0.5f) d.z = wallCoord - WallGap - b.max.z;
        else if (towardWall.z < -0.5f) d.z = wallCoord + WallGap - b.min.z;
        else if (towardWall.x > 0.5f) d.x = wallCoord - WallGap - b.max.x;
        else if (towardWall.x < -0.5f) d.x = wallCoord + WallGap - b.min.x;
        go.transform.position += d;
    }

    // 모델마다 앞쪽 방향이 달라서, 수평 레이로 더 깊이 들어가는 쪽(열린 면)을 찾아 toward 방향으로 맞춤
    private static void FaceOpenSide(GameObject go, Vector3 toward)
    {
        var temp = AddMeshColliders(go);
        float front = Openness(go, toward, temp);
        float back = Openness(go, -toward, temp);
        RemoveColliders(temp);
        bool flip = back > front + 0.01f;
        report.Add($"FACE {go.name}: toward={front:F2} opposite={back:F2} -> {(flip ? "rotated 180" : "kept")}");

        if (flip) go.transform.RotateAround(GetBounds(go).center, Vector3.up, 180f);
    }

    private static float Openness(GameObject go, Vector3 dir, List<MeshCollider> colliders)
    {
        Bounds b = GetBounds(go);
        Vector3 side = Vector3.Cross(Vector3.up, dir);
        float halfDepth = Vector3.Dot(b.extents, Abs(dir));
        float halfWidth = Vector3.Dot(b.extents, Abs(side));
        float maxDist = halfDepth * 2f;

        float total = 0f;
        int n = 0;
        for (int i = 1; i <= 5; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                Vector3 origin = b.center + dir * (halfDepth + 0.5f) + side * (halfWidth * 0.4f * j);
                origin.y = b.min.y + b.size.y * i / 6f;
                float dist = maxDist;
                if (NearestHit(origin, -dir, maxDist + 0.5f, colliders, out RaycastHit hit))
                    dist = Mathf.Min(dist, hit.distance - 0.5f);
                total += dist;
                n++;
            }
        }
        return total / n;
    }

    private static List<MeshCollider> AddMeshColliders(GameObject go)
    {
        var list = new List<MeshCollider>();
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null || mf.name == "MedicineBox") continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            list.Add(mc);
        }
        Physics.SyncTransforms();
        return list;
    }

    private static void RemoveColliders(List<MeshCollider> list)
    {
        foreach (var mc in list) Object.DestroyImmediate(mc);
    }

    private static void AddStaticCollider(string name, Transform parent, Bounds b)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = b.center;
        go.AddComponent<BoxCollider>().size = b.size;
    }

    // ===================== 약 (집을 수 있는 물건) =====================

    private static void MakeJar(Transform parent, string itemName, string prefabPath, Vector3 bottom)
    {
        var jar = Place(prefabPath, parent, bottom + Vector3.up * 0.005f, 0f, 0.16f);
        if (jar == null) return;

        Bounds b = GetBounds(jar);
        var col = jar.AddComponent<BoxCollider>();
        Vector3 s = jar.transform.lossyScale;
        col.center = jar.transform.InverseTransformPoint(b.center);
        col.size = new Vector3(b.size.x / s.x, b.size.y / s.y, b.size.z / s.z);
        // 프리팹에 들어 있는 오목한 MeshCollider는 움직이는 Rigidbody와 같이 쓸 수 없으므로 볼록으로
        foreach (var mc in jar.GetComponentsInChildren<MeshCollider>()) mc.convex = true;
        MakeGrabbable(jar, itemName);
    }

    private static void MakeBox(Transform parent, string itemName, Vector3 bottom, Vector3 size, Material mat)
    {
        var box = MakeCube(itemName, parent, bottom + Vector3.up * (size.y / 2f + 0.005f), size, mat, true);
        MakeGrabbable(box, itemName);
    }

    private static void MakeGrabbable(GameObject go, string itemName)
    {
        go.name = itemName;
        var grab = go.AddComponent<XRGrabInteractable>(); // Rigidbody 자동 추가
        grab.useDynamicAttach = true;
        go.GetComponent<Rigidbody>().mass = 0.2f;

        var item = go.AddComponent<PharmacyItem>();
        item.itemName = itemName;
        float h = GetBounds(go).size.y;
        item.labelHeight = h / 2f + 0.07f;
        item.label = MakeText(itemName + "_Label", go.transform.parent, itemName, go.transform.position + Vector3.up * item.labelHeight, 0.8f, new Color(0.15f, 0.15f, 0.15f)).transform;
    }

    // ===================== 직원 NPC (CafeBuilder에서도 사용) =====================

    private static Transform MakePharmacist(Transform parent, Vector3 feet) =>
        MakeStaff("Pharmacist", parent, feet, "NPC_Coat", new Color(0.97f, 0.97f, 0.97f));

    // 앞(-z)을 보는 사람 모양 NPC. 옷 색만 바꿔서 약사, 카페 사장님에 같이 씀
    internal static Transform MakeStaff(string name, Transform parent, Vector3 feet, string coatMat, Color coatColor)
    {
        var npc = Group(name, parent);
        npc.position = feet;

        Material coat = GetOrCreateMat(coatMat, coatColor);
        Material skin = GetOrCreateMat("NPC_Skin", new Color(1f, 0.85f, 0.72f));
        Material hair = GetOrCreateMat("NPC_Hair", new Color(0.2f, 0.15f, 0.12f));
        Material dark = GetOrCreateMat("NPC_Dark", new Color(0.1f, 0.1f, 0.1f));
        Material green = GetOrCreateMat("SignPlate", new Color(0.15f, 0.6f, 0.4f));

        MakePrimitive(PrimitiveType.Capsule, "Body", npc, new Vector3(0f, 0.7f, 0f), new Vector3(0.5f, 0.7f, 0.4f), coat);
        MakePrimitive(PrimitiveType.Sphere, "Head", npc, new Vector3(0f, 1.58f, 0f), new Vector3(0.32f, 0.32f, 0.32f), skin);
        MakePrimitive(PrimitiveType.Sphere, "Hair", npc, new Vector3(0f, 1.65f, 0.03f), new Vector3(0.34f, 0.22f, 0.33f), hair);
        MakePrimitive(PrimitiveType.Sphere, "Eye_L", npc, new Vector3(-0.06f, 1.6f, -0.145f), new Vector3(0.04f, 0.05f, 0.03f), dark);
        MakePrimitive(PrimitiveType.Sphere, "Eye_R", npc, new Vector3(0.06f, 1.6f, -0.145f), new Vector3(0.04f, 0.05f, 0.03f), dark);
        MakePrimitive(PrimitiveType.Cube, "Mouth", npc, new Vector3(0f, 1.5f, -0.15f), new Vector3(0.07f, 0.015f, 0.01f), dark);
        MakePrimitive(PrimitiveType.Cube, "Badge", npc, new Vector3(-0.12f, 1.2f, -0.2f), new Vector3(0.08f, 0.08f, 0.01f), green);
        return npc;
    }

    // NPC를 잡거나(Select) 옆의 "말 걸기" 버튼을 누르면 대화가 시작되도록 설정
    internal static StaffTalk MakeTalkable(Transform npc, string buttonLabel, Vector3 buttonPos)
    {
        var col = npc.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f);
        col.height = 1.8f;
        col.radius = 0.3f;
        var talk = npc.gameObject.AddComponent<StaffTalk>(); // XRSimpleInteractable 자동 추가

        var canvas = MakeCanvas(npc.name + "_Talk_Canvas", npc.parent, buttonPos, new Vector2(420f, 120f), 0.0012f);
        talk.talkButton = MakeButton("Talk", canvas, buttonLabel, Vector2.zero, new Vector2(420f, 120f), new Color(1f, 0.9f, 0.45f));
        return talk;
    }

    // ===================== UI (CafeBuilder에서도 사용) =====================

    internal static void LoadUiAssets()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    internal static RectTransform MakeCanvas(string name, Transform parent, Vector3 pos, Vector2 size, float scale)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
        go.transform.SetParent(parent, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.position = pos;
        rt.localScale = Vector3.one * scale;
        return rt;
    }

    internal static RectTransform MakePanel(string name, RectTransform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = roundSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.15f; // 모서리를 더 둥글게
        img.color = color;
        return rt;
    }

    internal static TextMeshProUGUI MakeUIText(string name, RectTransform parent, string text, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true; // 글이 길어서 칸을 넘칠 때만 줄어듦
        tmp.fontSizeMin = fontSize * 0.6f;
        tmp.fontSizeMax = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    internal static Button MakeButton(string name, RectTransform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var rt = MakePanel(name, parent, pos, size, color);
        var button = rt.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        button.colors = colors;
        MakeUIText("Label", rt, label, Vector2.zero, size - new Vector2(20f, 10f), 50f, new Color(0.15f, 0.15f, 0.15f));
        return button;
    }

    private static TextMeshPro MakeText(string name, Transform parent, string text, Vector3 pos, float size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
        return tmp;
    }

    // ===================== 연출 =====================

    private static ParticleSystem MakeConfetti(Transform parent, Vector3 pos)
    {
        var go = new GameObject("Confetti");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // 위로 발사

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.5f;
        main.maxParticles = 300;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.98f, 0.85f, 0.4f), 0f),
                new GradientColorKey(new Color(0.95f, 0.6f, 0.7f), 0.33f),
                new GradientColorKey(new Color(0.55f, 0.75f, 0.95f), 0.66f),
                new GradientColorKey(new Color(0.55f, 0.85f, 0.6f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 150) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.2f;

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetOrCreateMat("Confetti", Color.white, "Universal Render Pipeline/Particles/Unlit");
        return ps;
    }

    // ===================== 공통 =====================

    private static Transform Group(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    private static GameObject MakePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static GameObject MakeCube(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static Material GetOrCreateMat(string name, Color color, string shader = "Universal Render Pipeline/Lit")
    {
        if (!AssetDatabase.IsValidFolder(MatDir))
            AssetDatabase.CreateFolder("Assets/04_Resource", "Pharmacy");

        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Bounds GetBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds b = default;
        bool any = false;
        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer || r.GetComponent<TMP_Text>() != null) continue;
            if (!any) { b = r.bounds; any = true; }
            else b.Encapsulate(r.bounds);
        }
        return any ? b : new Bounds(go.transform.position, Vector3.zero);
    }

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private static float Lerp(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
