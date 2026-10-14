using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 길안내 메뉴(Scene2 상단 [길안내] 버튼): 카페/약국으로 안내를 시작하거나 끔.
/// 같은 자리에 뜨는 다른 메뉴가 열리면 자동으로 닫힘 (SettingsPanel과 같은 방식).
/// </summary>
public class GuideMenu : MonoBehaviour
{
    public GameObject panel;
    public GuideArrow arrow;
    public Button cafeButton;
    public Button pharmacyButton;
    public Button offButton;
    public Button closeButton;
    public GameObject[] hideWhileOpen;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = panel.transform.localScale;
        cafeButton.onClick.AddListener(() => Pick(0));
        pharmacyButton.onClick.AddListener(() => Pick(1));
        offButton.onClick.AddListener(() => { arrow.Stop(); Close(); });
        closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    private void Update()
    {
        if (!panel.activeSelf) return;
        foreach (var go in hideWhileOpen)
            if (go != null && go.activeSelf) { Close(); return; }
    }

    public void Toggle()
    {
        if (panel.activeSelf) Close(); else Open();
    }

    public void Open()
    {
        foreach (var go in hideWhileOpen) if (go != null) go.SetActive(false);
        panel.SetActive(true);
        panel.transform.DOKill();
        panel.transform.localScale = baseScale * 0.85f;
        panel.transform.DOScale(baseScale, 0.25f).SetEase(Ease.OutBack);
    }

    public void Close()
    {
        panel.transform.DOKill();
        panel.transform.localScale = baseScale;
        panel.SetActive(false);
    }

    private void Pick(int index)
    {
        arrow.SetTarget(index);
        Close();
    }

    private void OnDestroy()
    {
        if (panel != null) panel.transform.DOKill();
    }
}
