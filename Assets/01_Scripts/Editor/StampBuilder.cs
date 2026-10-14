using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬마다 스탬프 획득 알림(Stamp_Toast)을 붙이고, Scene2(먹자골목)에는 OX 퀴즈 코너와 골목 도착 스탬프를 만듦.
/// SettingsBuilder의 [타이틀 + 설정 전체 씬에 적용]에서 같이 실행되고, 다시 실행하면 기존 것을 지우고 새로 만듦.
/// </summary>
public static class StampBuilder
{
    private const string ToastName = "Stamp_Toast";
    private const string QuizName = "Quiz";
    private const string TriggerName = "Street_Stamp";

    private static readonly Color Ink = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color Brown = new Color(0.55f, 0.35f, 0.2f);
    private static readonly Color Cream = new Color(1f, 0.97f, 0.9f, 0.95f);

    internal static void Build(Scene scene)
    {
        PharmacyBuilder.LoadUiAssets();
        AddToast(scene);
        if (scene.name == "Scene2") BuildQuiz(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // 머리에 붙은 "스탬프 획득!" 알림. 이동 UI보다 조금 위
    private static void AddToast(Scene scene)
    {
        var old = SettingsBuilder.Find(scene, ToastName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var cam = SettingsBuilder.Find(scene, "Main Camera");
        var moveUI = SettingsBuilder.Find(scene, "MoveUI");
        if (cam == null || moveUI == null) return;

        float z = cam.InverseTransformPoint(moveUI.position).z;
        var canvas = PharmacyBuilder.MakeCanvas(ToastName, cam, cam.TransformPoint(new Vector3(0f, 0.2f, z)), new Vector2(360f, 110f), moveUI.lossyScale.x);
        var toast = canvas.gameObject.AddComponent<StampToast>();

        var audio = canvas.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PharmacyBuilder.SfxPath);
        toast.sfx = audio;

        var panel = PharmacyBuilder.MakePanel("Panel", canvas, Vector2.zero, new Vector2(360f, 110f), new Color(1f, 0.85f, 0.4f, 0.95f));
        toast.panel = panel.gameObject;
        toast.group = panel.gameObject.AddComponent<CanvasGroup>();
        toast.label = PharmacyBuilder.MakeUIText("Label", panel, "", Vector2.zero, new Vector2(340f, 95f), 26f, Ink);
        EditorUtility.SetDirty(toast);
    }

    private static void BuildQuiz(Scene scene)
    {
        foreach (var name in new[] { QuizName, TriggerName })
        {
            var old = SettingsBuilder.Find(scene, name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        var cam = SettingsBuilder.Find(scene, "Main Camera");
        if (cam == null) return;

        // 시작 위치에서 보는 방향 앞쪽 1.8m (길안내 화살표가 1.2m 앞에 뜨므로 겹치지 않게), 눈높이보다 살짝 아래에 퀴즈판을 세움
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 pos = cam.position + fwd * 1.8f + Vector3.down * 0.1f;

        var quiz = new GameObject(QuizName).AddComponent<QuizGame>();
        var audio = quiz.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PharmacyBuilder.SfxPath);
        quiz.sfx = audio;

        var canvas = PharmacyBuilder.MakeCanvas("Quiz_Canvas", quiz.transform, pos, new Vector2(1000f, 700f), 0.0017f);
        canvas.rotation = Quaternion.LookRotation(fwd);

        // ----- 시작 화면 -----
        var intro = PharmacyBuilder.MakePanel("Intro_Panel", canvas, Vector2.zero, new Vector2(1000f, 700f), Cream);
        PharmacyBuilder.MakeUIText("Title", intro, "먹자골목 퀴즈", new Vector2(0f, 190f), new Vector2(900f, 130f), 96f, Brown);
        PharmacyBuilder.MakeUIText("Sub", intro, "3문제 중 2문제 이상 맞히면\n스탬프를 받아요!", new Vector2(0f, 20f), new Vector2(900f, 180f), 54f, Ink);
        quiz.startButton = PharmacyBuilder.MakeButton("StartBtn", intro, "퀴즈 시작", new Vector2(0f, -210f), new Vector2(440f, 120f), new Color(0.55f, 0.85f, 0.6f));
        quiz.introPanel = intro.gameObject;

        // ----- 문제 화면 -----
        var question = PharmacyBuilder.MakePanel("Question_Panel", canvas, Vector2.zero, new Vector2(1000f, 700f), Cream);
        quiz.progressText = PharmacyBuilder.MakeUIText("Progress", question, "", new Vector2(0f, 290f), new Vector2(600f, 60f), 44f, Brown);
        quiz.questionText = PharmacyBuilder.MakeUIText("Question", question, "", new Vector2(0f, 130f), new Vector2(920f, 260f), 56f, Ink);
        quiz.oButton = PharmacyBuilder.MakeButton("O_Btn", question, "O", new Vector2(-230f, -170f), new Vector2(380f, 230f), new Color(0.6f, 0.8f, 1f));
        quiz.xButton = PharmacyBuilder.MakeButton("X_Btn", question, "X", new Vector2(230f, -170f), new Vector2(380f, 230f), new Color(1f, 0.65f, 0.6f));
        foreach (var b in new[] { quiz.oButton, quiz.xButton })
        {
            var tmp = b.GetComponentInChildren<TMP_Text>();
            tmp.fontSizeMax = 140f;
            tmp.fontSize = 140f;
        }
        quiz.questionPanel = question.gameObject;

        // 정답 확인: 문제 패널의 O/X 버튼 자리를 덮음
        var feedback = PharmacyBuilder.MakePanel("Feedback_Panel", canvas, new Vector2(0f, -150f), new Vector2(960f, 380f), new Color(1f, 0.93f, 0.75f, 1f));
        quiz.feedbackText = PharmacyBuilder.MakeUIText("Feedback", feedback, "", new Vector2(0f, 60f), new Vector2(900f, 240f), 50f, Ink);
        quiz.nextButton = PharmacyBuilder.MakeButton("NextBtn", feedback, "다음 문제", new Vector2(0f, -125f), new Vector2(380f, 100f), new Color(0.55f, 0.85f, 0.6f));
        quiz.nextLabel = quiz.nextButton.GetComponentInChildren<TMP_Text>();
        quiz.feedbackPanel = feedback.gameObject;

        // ----- 결과 화면 -----
        var result = PharmacyBuilder.MakePanel("Result_Panel", canvas, Vector2.zero, new Vector2(1000f, 700f), Cream);
        quiz.resultText = PharmacyBuilder.MakeUIText("Result", result, "", new Vector2(0f, 80f), new Vector2(920f, 360f), 62f, Brown);
        quiz.retryButton = PharmacyBuilder.MakeButton("RetryBtn", result, "다시 풀기", new Vector2(0f, -230f), new Vector2(400f, 110f), new Color(0.98f, 0.8f, 0.45f));
        quiz.resultPanel = result.gameObject;

        question.gameObject.SetActive(false);
        feedback.gameObject.SetActive(false);
        result.gameObject.SetActive(false);

        // 먹자골목에 도착하면 스탬프
        var trigger = new GameObject(TriggerName).AddComponent<StampTrigger>();
        trigger.stamp = Stamp.Street;

        EditorUtility.SetDirty(quiz);
    }
}
