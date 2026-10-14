using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시선 응시 정보 포인트: 표시된 곳을 dwellSeconds 동안 쳐다보면 원형 게이지가 차오르고, 다 차면 설명 카드가 뜸.
/// 시선을 돌리면 게이지가 줄어들고, 카드는 잠깐 뒤에 닫힘. 마커와 카드는 항상 보는 사람을 향함.
/// </summary>
public class GazeHotspot : MonoBehaviour
{
    public string title;
    [TextArea] public string description;

    public Transform canvasRoot;    // 마커와 카드가 들어있는 월드 캔버스
    public RectTransform ring;      // 마커(바깥 고리) - 살짝 커졌다 작아지며 시선을 끎
    public Image gauge;             // 차오르는 게이지 (Radial 360)
    public Transform cardRoot;      // 설명 카드 캔버스. 마커와 따로 두어 360° 구체 안쪽으로 당겨 놓음
    public GameObject card;         // 카드 본체 (cardRoot 아래)
    public float cardDistanceRatio = 0.65f; // 마커 거리(구체 표면)의 몇 배 지점에 카드를 둘지 (표면에 두면 구체에 잘림)
    public float cardRise = 0.32f;          // 마커보다 위로 띄우는 높이 (원래 크기 기준)
    public TMP_Text titleText;
    public TMP_Text bodyText;

    public float dwellSeconds = 2f;
    public float gazeAngle = 6f;     // 시선과 마커 방향의 차이가 이 각도 안이면 "보고 있음"
    public float closeDelay = 1.2f;  // 카드가 열린 뒤 시선을 돌리고 이만큼 지나면 닫힘

    private float progress;
    private bool open;
    private float lostTime;

    private void Start()
    {
        titleText.text = title;
        bodyText.text = description;
        card.SetActive(false);
        gauge.fillAmount = 0f;
        ring.DOScale(1.12f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    private void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 to = transform.position - cam.transform.position;
        bool gazing = Vector3.Angle(cam.transform.forward, to) < gazeAngle;

        if (gazing)
        {
            lostTime = 0f;
            if (!open)
            {
                progress = Mathf.Min(1f, progress + Time.deltaTime / dwellSeconds);
                if (progress >= 1f) Open();
            }
        }
        else
        {
            lostTime += Time.deltaTime;
            if (open)
            {
                if (lostTime > closeDelay) Close();
            }
            else
            {
                progress = Mathf.Max(0f, progress - Time.deltaTime * 1.5f / dwellSeconds);
            }
        }

        gauge.fillAmount = progress;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        canvasRoot.rotation = Quaternion.LookRotation(canvasRoot.position - cam.transform.position);

        // 카드는 카메라 쪽으로 당겨서(같은 방향이라 화면에서는 같은 자리) 구체 표면 밖으로 삐져나가 잘리지 않게 함
        float k = cardDistanceRatio;
        Vector3 camPos = cam.transform.position;
        cardRoot.position = camPos + (canvasRoot.position - camPos) * k + Vector3.up * (cardRise * k);
        cardRoot.rotation = Quaternion.LookRotation(cardRoot.position - camPos);
    }

    private void Open()
    {
        open = true;
        card.transform.DOKill();
        card.SetActive(true);
        card.transform.localScale = Vector3.one * 0.7f;
        card.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void Close()
    {
        open = false;
        progress = 0f;
        card.transform.DOKill();
        card.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ring != null) ring.DOKill();
        if (card != null) card.transform.DOKill();
    }
}
