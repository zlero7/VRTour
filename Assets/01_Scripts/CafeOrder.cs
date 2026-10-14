using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카페(Scene1, 360도 식당) 메뉴 주문: 사장님에게 말을 걸면 메뉴판이 나오고,
/// 고른 메뉴를 TourState에 저장 → 약국(Scene3)에서 증상과 결과로 이어짐
/// </summary>
public class CafeOrder : MonoBehaviour
{
    [System.Serializable]
    public class Menu
    {
        public string name;       // 버튼에 보이는 메뉴 이름
        public string reply;      // 사장님 대답
        public int symptomIndex;  // 먹고 나서 생기는 증상 (PharmacyGame.symptoms 인덱스)
        public string complaint;  // 약국에서 플레이어가 하는 말 (증상 선택 버튼)
        public string cure;       // 그 말을 들은 약사 대답
    }

    public Menu[] menus =
    {
        new Menu { name = "해물파전", reply = "바삭하게 부쳐 드릴게요!\n양이 많으니 천천히 드세요.", symptomIndex = 0,
            complaint = "파전을 많이 먹어서\n배가 더부룩해요", cure = "기름진 파전을 많이 드셨군요!" },
        new Menu { name = "아이스 아메리카노", reply = "진하게 샷 추가해 드릴게요!\n카페인이 좀 셀 거예요.", symptomIndex = 1,
            complaint = "커피를 마셨더니\n머리가 아파요", cure = "카페인 때문에 그럴 수 있어요." },
        new Menu { name = "떡볶이", reply = "오늘 떡볶이가 좀 매워요!\n물 많이 드세요.", symptomIndex = 2,
            complaint = "떡볶이를 먹었더니\n목이 칼칼해요", cure = "떡볶이가 많이 매웠나 봐요!" },
    };

    [Header("UI")]
    public GameObject speechBubble; // 말풍선 (이름표, 대사 포함)
    public TMP_Text speechText;
    public GameObject menuPanel;
    public Button[] menuButtons;
    public GameObject donePanel;
    public TMP_Text doneText;
    public Button pharmacyButton;
    public Button reorderButton;

    [Header("연출")]
    public StaffTalk staff;
    public AudioSource sfx;

    private Menu ordered;
    private Coroutine typing;

    private void Start()
    {
        // 카페에 들어올 때마다 새로 주문 (이전 주문과 대사는 남기지 않음)
        TourState.Reset();

        for (int i = 0; i < menuButtons.Length && i < menus.Length; i++)
        {
            int index = i;
            menuButtons[i].GetComponentInChildren<TMP_Text>().text = menus[i].name;
            menuButtons[i].onClick.AddListener(() => Order(index));
        }
        pharmacyButton.onClick.AddListener(() => VRSceneFader.LoadScene("Scene3"));
        reorderButton.onClick.AddListener(Reorder);
        if (staff != null) staff.Talked += OnTalk;

        // 사장님에게 말을 걸기 전까지는 말풍선과 메뉴판을 숨김
        speechBubble.SetActive(false);
        menuPanel.SetActive(false);
        donePanel.SetActive(false);
    }

    private void OnTalk()
    {
        PlaySfx(1f);
        if (ordered == null)
        {
            Say("어서 오세요, 먹자골목 카페입니다!\n무엇을 주문하시겠어요?");
            if (!menuPanel.activeSelf) Pop(menuPanel);
        }
        else
        {
            Say($"{ordered.name} 맛있게 드세요!\n속이 불편하면 근처 불광 약국에 가보세요.");
        }
    }

    private void Order(int index)
    {
        PlaySfx(1.2f);
        ordered = menus[index];
        TourState.SetCafeOrder(ordered.name, ordered.symptomIndex, ordered.complaint, ordered.cure);

        menuPanel.SetActive(false);
        Say($"{ordered.name} 나왔습니다!\n{ordered.reply}");
        doneText.text = $"주문한 메뉴: {ordered.name}\n드시고 나서 불편하면 불광 약국에 가보세요!";
        Pop(donePanel);
    }

    private void Reorder()
    {
        PlaySfx(1f);
        ordered = null;
        TourState.Reset();
        donePanel.SetActive(false);
        Say("다른 메뉴로 바꿔 드릴게요!\n무엇을 드릴까요?");
        Pop(menuPanel);
    }

    // 패널이 톡 튀어나오는 연출
    private static void Pop(GameObject panel)
    {
        Transform t = panel.transform;
        t.DOKill();
        panel.SetActive(true);
        t.localScale = Vector3.one * 0.8f;
        t.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    private void Say(string text)
    {
        speechBubble.SetActive(true);
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeText(text));
    }

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

    private void PlaySfx(float pitch)
    {
        if (sfx == null || sfx.clip == null) return;
        sfx.pitch = pitch;
        sfx.PlayOneShot(sfx.clip);
    }

    private void OnDestroy()
    {
        if (staff != null) staff.Talked -= OnTalk;
        menuPanel.transform.DOKill();
        donePanel.transform.DOKill();
    }
}
