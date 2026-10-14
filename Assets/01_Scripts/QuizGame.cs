using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 먹자골목 OX 퀴즈: 문제를 풀 때마다 정답 여부와 해설을 보여주고, 마지막에 점수로 결과를 알려줌.
/// passMark 이상 맞히면 "퀴즈 풀이" 스탬프를 얻음. 문제는 인스펙터에서 바꿀 수 있음.
/// </summary>
public class QuizGame : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        [TextArea] public string text;
        public bool answer;          // true = O, false = X
        [TextArea] public string explain;
    }

    public Question[] questions =
    {
        new Question { text = "매운 음식을 먹었을 때는\n물보다 우유가 매운맛을 더 잘 가라앉힌다.", answer = true,
            explain = "매운맛 성분은 물에 잘 안 녹고\n우유의 지방과 단백질이 도움을 줘요." },
        new Question { text = "약은 정해진 양보다 많이 먹어도\n더 빨리 낫는다.", answer = false,
            explain = "약은 정해진 양과 시간을 지켜야 해요.\n많이 먹으면 오히려 위험해요!" },
        new Question { text = "해물파전은 밀가루 반죽에\n쪽파와 해물을 넣어 부친 음식이다.", answer = true,
            explain = "바삭하게 부친 파전은\n막걸리와도 잘 어울리는 먹자골목 대표 메뉴예요." },
    };
    public int passMark = 2;

    [Header("UI")]
    public GameObject introPanel;
    public GameObject questionPanel;
    public GameObject feedbackPanel;
    public GameObject resultPanel;
    public Button startButton;
    public Button oButton;
    public Button xButton;
    public Button nextButton;
    public TMP_Text nextLabel;
    public Button retryButton;
    public TMP_Text progressText;
    public TMP_Text questionText;
    public TMP_Text feedbackText;
    public TMP_Text resultText;
    public AudioSource sfx;

    private int index;
    private int score;
    private bool answered;

    private void Start()
    {
        startButton.onClick.AddListener(StartQuiz);
        oButton.onClick.AddListener(() => Answer(true));
        xButton.onClick.AddListener(() => Answer(false));
        nextButton.onClick.AddListener(Next);
        retryButton.onClick.AddListener(StartQuiz);
        ShowOnly(introPanel);
    }

    private void StartQuiz()
    {
        PlaySfx(1f);
        index = 0;
        score = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        answered = false;
        var q = questions[index];
        progressText.text = $"문제 {index + 1}/{questions.Length}";
        questionText.text = q.text;
        ShowOnly(questionPanel);
        feedbackPanel.SetActive(false);
        Pop(questionPanel);
    }

    private void Answer(bool o)
    {
        if (answered) return;
        answered = true;

        var q = questions[index];
        bool correct = o == q.answer;
        if (correct) score++;

        PlaySfx(correct ? 1.3f : 0.7f);
        feedbackText.text = (correct ? "정답!" : "오답!") + "\n" + q.explain;
        nextLabel.text = index == questions.Length - 1 ? "결과 보기" : "다음 문제";
        Pop(feedbackPanel);
        if (!correct) questionPanel.transform.DOShakePosition(0.4f, new Vector3(12f, 0f, 0f), 20);
    }

    private void Next()
    {
        PlaySfx(1f);
        index++;
        if (index < questions.Length) ShowQuestion();
        else ShowResult();
    }

    private void ShowResult()
    {
        bool pass = score >= passMark;
        StampBook.QuizTotal = questions.Length;
        StampBook.QuizScore = Mathf.Max(StampBook.QuizScore, score);

        string message = pass
            ? (StampBook.Has(Stamp.Quiz) ? "잘했어요! 스탬프는 이미 가지고 있어요." : "스탬프를 받았어요!")
            : $"{passMark}문제 이상 맞히면 스탬프를 받아요. 다시 도전해 보세요!";
        resultText.text = $"퀴즈 결과  {score}/{questions.Length}\n{message}";

        ShowOnly(resultPanel);
        Pop(resultPanel);
        if (pass) StampBook.Collect(Stamp.Quiz);
    }

    private void ShowOnly(GameObject panel)
    {
        introPanel.SetActive(panel == introPanel);
        questionPanel.SetActive(panel == questionPanel);
        resultPanel.SetActive(panel == resultPanel);
        if (panel != questionPanel) feedbackPanel.SetActive(false);
    }

    private static void Pop(GameObject panel)
    {
        Transform t = panel.transform;
        t.DOKill();
        panel.SetActive(true);
        t.localScale = Vector3.one * 0.85f;
        t.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    private void PlaySfx(float pitch)
    {
        if (sfx == null || sfx.clip == null) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(sfx.clip);
    }

    private void OnDestroy()
    {
        foreach (var p in new[] { introPanel, questionPanel, feedbackPanel, resultPanel })
            if (p != null) p.transform.DOKill();
    }
}
