using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면(Main 씬): 제목이 떠오르며 나타나고 [투어 시작] / [장소 선택] / [설정] 버튼을 보여줌.
/// 설정을 열면 타이틀은 숨기고, 설정을 닫으면 다시 보여줌.
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public GameObject titlePanel;
    public CanvasGroup titleGroup;
    public RectTransform titleBlock;   // 위아래로 둥실거리는 제목 묶음
    public Button startButton;
    public Button placesButton;
    public Button settingsButton;
    public GameObject placesMenu;      // 기존 "이동하기" 메뉴 (MoveUI)
    public SettingsPanel settings;
    public Button stampsButton;
    public GameObject stampPanel;      // 스탬프 판
    public Button stampCloseButton;
    public string startScene = "Scene2";

    private void Start()
    {
        if (placesMenu != null) placesMenu.SetActive(false);
        stampPanel.SetActive(false);

        startButton.onClick.AddListener(StartTour);
        placesButton.onClick.AddListener(OpenPlaces);
        stampsButton.onClick.AddListener(OpenStamps);
        stampCloseButton.onClick.AddListener(ShowTitle);
        settingsButton.onClick.AddListener(() => settings.Open());
        settings.Closed += ShowTitle;
        settings.hideWhileOpen = new[] { titlePanel, placesMenu, stampPanel };

        // 등장 연출: 서서히 나타나며 커지고, 이후 제목이 둥실거림
        titleGroup.alpha = 0f;
        titlePanel.transform.localScale = Vector3.one * 0.9f;
        titleGroup.DOFade(1f, 0.8f).SetDelay(0.3f);
        titlePanel.transform.DOScale(1f, 0.8f).SetDelay(0.3f).SetEase(Ease.OutBack);
        titleBlock.DOAnchorPosY(titleBlock.anchoredPosition.y + 14f, 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    private void StartTour()
    {
        // 스탬프를 다 모은 뒤 다시 시작하면 새 투어로 보고 처음부터 다시 모음 (타이틀에서는 모은 스탬프를 그대로 보여줌)
        if (StampBook.IsComplete) StampBook.ResetAll();
        VRSceneFader.LoadScene(startScene);
    }

    private void OpenPlaces()
    {
        titlePanel.SetActive(false);
        placesMenu.SetActive(true);
    }

    private void OpenStamps()
    {
        titlePanel.SetActive(false);
        stampPanel.SetActive(true);
        stampPanel.transform.DOKill();
        stampPanel.transform.localScale = Vector3.one * 0.9f;
        stampPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    // 장소 선택 메뉴의 닫기(X) 버튼, 스탬프 판의 돌아가기 버튼에서 호출
    public void ShowTitle()
    {
        if (placesMenu != null) placesMenu.SetActive(false);
        stampPanel.SetActive(false);
        titlePanel.SetActive(true);
    }

    private void OnDestroy()
    {
        if (settings != null) settings.Closed -= ShowTitle;
        if (titleBlock != null) titleBlock.DOKill();
        if (titleGroup != null) titleGroup.DOKill();
        if (titlePanel != null) titlePanel.transform.DOKill();
        if (stampPanel != null) stampPanel.transform.DOKill();
    }
}
