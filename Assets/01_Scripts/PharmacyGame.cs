using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 약국 체험 진행: 약사에게 증상 말하기 → 맞는 약 찾기 → 계산대에 올리기 → 투어 완료
/// 카페(Scene1)에서 메뉴를 골랐으면 그 메뉴 때문에 생긴 증상으로 바로 시작하고, 결과 화면에도 남김
/// </summary>
public class PharmacyGame : MonoBehaviour
{
    [System.Serializable]
    public class Symptom
    {
        public string say;      // 버튼에 보이는 말
        public string reply;    // 약사 대답
        public string medicine; // 필요한 약 (PharmacyItem.itemName과 같아야 함)
        public string where;    // 약 위치 안내
    }

    public Symptom[] symptoms =
    {
        new Symptom { say = "배가 더부룩해요", reply = "먹자골목에서 과식하셨군요!", medicine = "소화제", where = "오른쪽 진열대" },
        new Symptom { say = "머리가 아파요", reply = "북적이는 골목이라 피곤하셨나 봐요.", medicine = "두통약", where = "가운데 테이블" },
        new Symptom { say = "목이 칼칼해요", reply = "떡볶이가 매웠나 봐요!", medicine = "목캔디", where = "왼쪽 진열대" },
    };

    [Header("UI")]
    public GameObject speechBubble; // 약사 말풍선
    public TMP_Text speechText;
    public GameObject choicePanel;
    public Button[] choiceButtons;
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button homeButton;
    public Button retryButton;
    public TMP_Text hintText;

    [Header("연출")]
    public Transform npc;
    public StaffTalk staff;         // 약사에게 말 걸기
    public ParticleSystem confetti;
    public AudioSource sfx;

    private string target;
    private bool done;
    private int mistakes; // 틀린 약을 올린 횟수 (별점에 반영)
    private int chosen = -1; // 고른 증상
    private string cafeMenu;
    private int cafeSymptom = -1;
    private string cafeCure;
    private float startTime;
    private Coroutine typing;
    private Tween shakeTween;

    private void Start()
    {
        startTime = Time.time;
        resultPanel.SetActive(false);

        for (int i = 0; i < choiceButtons.Length && i < symptoms.Length; i++)
        {
            int index = i;
            choiceButtons[i].GetComponentInChildren<TMP_Text>().text = symptoms[i].say;
            choiceButtons[i].onClick.AddListener(() => Choose(index));
        }
        homeButton.onClick.AddListener(() => VRSceneFader.LoadScene("Main"));
        retryButton.onClick.AddListener(() => VRSceneFader.LoadScene(SceneManager.GetActiveScene().name));

        if (npc != null) npc.DOLocalMoveY(npc.localPosition.y + 0.02f, 1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

        // 카페 주문은 이번 방문에서 한 번만 쓰고 지움 (다른 곳에 갔다 오면 처음부터)
        cafeMenu = TourState.CafeMenu;
        cafeSymptom = TourState.VisitedCafe ? TourState.SymptomIndex : -1;
        cafeCure = TourState.Cure;
        // 카페에서 먹은 메뉴 때문에 생긴 증상은 선택지 문구를 "떡볶이를 먹었더니 목이 칼칼해요"처럼 바꿈
        if (cafeSymptom >= 0 && cafeSymptom < choiceButtons.Length && !string.IsNullOrEmpty(TourState.Complaint))
            choiceButtons[cafeSymptom].GetComponentInChildren<TMP_Text>().text = TourState.Complaint;
        TourState.Reset();

        // 약사에게 말을 걸기 전까지는 말풍선과 증상 선택을 숨김
        speechBubble.SetActive(false);
        choicePanel.SetActive(false);
        if (hintText != null) hintText.text = "약사에게 말을 걸어보세요";
        if (staff != null) staff.Talked += OnTalk;
    }

    private void OnTalk()
    {
        if (done) return;
        PlaySfx(1f);

        if (chosen >= 0)
        {
            // 이미 증상을 말했으면 약 위치를 다시 알려줌
            Say($"{WithJosa(target, "은", "는")} {symptoms[chosen].where}에 있어요.\n찾아서 계산대에 올려주세요!");
        }
        else
        {
            Say("어서 오세요, 불광 약국입니다!\n어디가 불편해서 오셨어요?");
            choicePanel.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (staff != null) staff.Talked -= OnTalk;
        if (npc != null) npc.DOKill();
    }

    private void Choose(int index)
    {
        if (done) return;
        PlaySfx(1f);

        Symptom s = symptoms[index];
        chosen = index;
        target = s.medicine;
        choicePanel.SetActive(false);
        string reply = index == cafeSymptom && !string.IsNullOrEmpty(cafeCure) ? cafeCure : s.reply;
        Say($"{reply}\n{WithJosa(target, "은", "는")} {s.where}에 있어요.\n찾아서 계산대에 올려주세요!");
        if (hintText != null) hintText.text = $"{WithJosa(target, "을", "를")} 찾아\n계산대에 올려주세요";
    }

    public void OnItemPlaced(PharmacyItem item)
    {
        if (done) return;

        if (target == null)
        {
            Say("먼저 약사에게 말을 걸어\n어디가 불편한지 말씀해 주세요!");
            ShakeNpc();
            return;
        }

        if (item.itemName != target)
        {
            mistakes++;
            PlaySfx(0.7f);
            Say($"이건 {item.itemName}{(HasBatchim(item.itemName) ? "이에요" : "예요")}.\n{WithJosa(target, "을", "를")} 찾아주세요!");
            ShakeNpc();
            return;
        }

        done = true;
        PlaySfx(1.3f);
        Say($"{target} 3,000원입니다.\n감사합니다, 몸조리 잘하세요!");
        if (confetti != null) confetti.Play();
        if (npc != null)
        {
            npc.DOKill();
            npc.DOPunchPosition(Vector3.up * 0.15f, 0.5f, 2);
        }
        if (hintText != null) hintText.gameObject.SetActive(false);
        StartCoroutine(ShowResult());
    }

    private IEnumerator ShowResult()
    {
        yield return new WaitForSeconds(2f);

        int sec = Mathf.RoundToInt(Time.time - startTime);
        int stars = Mathf.Clamp(3 - mistakes, 1, 3);
        string cafe = !string.IsNullOrEmpty(cafeMenu) ? $"카페에서 {WithJosa(cafeMenu, "을", "를")} 주문하고" : "카페는 들르지 않고";
        resultText.text = $"불광동 먹자골목 투어 완료!\n{cafe} 약국에서 {WithJosa(target, "을", "를")} 샀어요\n" +
                          $"별점 {stars}/3 (실수 {mistakes}번) / 소요 시간 {sec / 60:00}:{sec % 60:00}";

        Transform t = resultPanel.transform;
        Vector3 scale = t.localScale;
        resultPanel.SetActive(true);
        t.localScale = Vector3.zero;
        t.DOScale(scale, 0.4f).SetEase(Ease.OutBack);
    }

    private void Say(string text)
    {
        speechBubble.SetActive(true);
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeText(text));
    }

    // 말풍선 글자가 한 글자씩 나오는 연출
    private IEnumerator TypeText(string text)
    {
        speechText.text = text;
        speechText.maxVisibleCharacters = 0;
        speechText.ForceMeshUpdate();
        int total = speechText.textInfo.characterCount;
        for (int i = 1; i <= total; i++)
        {
            speechText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(0.03f);
        }
    }

    private void ShakeNpc()
    {
        if (npc == null) return;
        shakeTween?.Complete();
        shakeTween = npc.DOShakeRotation(0.5f, new Vector3(0f, 25f, 0f), 8);
    }

    private void PlaySfx(float pitch)
    {
        if (sfx == null || sfx.clip == null) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(sfx.clip);
    }

    private static bool HasBatchim(string word)
    {
        char c = word[word.Length - 1];
        return c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 != 0;
    }

    // 받침에 따라 은/는, 을/를 붙이기
    private static string WithJosa(string word, string withBatchim, string withoutBatchim)
    {
        return word + (HasBatchim(word) ? withBatchim : withoutBatchim);
    }
}
