using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널: 배경음/효과음 볼륨 슬라이더와 연속 이동 켜기/끄기. 값은 GameSettings에 저장됨.
/// 패널이 열려 있는 동안 hideWhileOpen(이동 메뉴 등)은 숨김.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    public GameObject panel;               // 열고 닫는 패널 본체
    public Slider bgmSlider;
    public Slider sfxSlider;
    public TMP_Text bgmValue;
    public TMP_Text sfxValue;
    public Button moveButton;
    public TMP_Text moveLabel;
    public Button closeButton;
    public AudioSource preview;            // 효과음 슬라이더를 움직일 때 들려줄 소리
    public GameObject[] hideWhileOpen;

    public event System.Action Closed;

    private Vector3 baseScale;
    private float lastPreview;

    private void Awake()
    {
        baseScale = panel.transform.localScale;
        bgmSlider.onValueChanged.AddListener(OnBgm);
        sfxSlider.onValueChanged.AddListener(OnSfx);
        moveButton.onClick.AddListener(() => { GameSettings.SetContinuousMove(!GameSettings.ContinuousMove); Refresh(); });
        closeButton.onClick.AddListener(Close);
        panel.SetActive(false);
    }

    // 설정이 열린 채로 이동 메뉴 등이 열리면 같은 자리에서 겹치므로 설정을 닫음
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
        Refresh();
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
        Closed?.Invoke();
    }

    // 슬라이더를 코드로 맞출 땐 이벤트가 다시 설정을 저장하지 않게 SetValueWithoutNotify 사용
    private void Refresh()
    {
        bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);
        sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        bgmValue.text = Percent(GameSettings.BgmVolume);
        sfxValue.text = Percent(GameSettings.SfxVolume);
        moveLabel.text = GameSettings.ContinuousMove ? "이동: 연속 이동 + 순간이동" : "이동: 순간이동만";
    }

    private void OnBgm(float v)
    {
        GameSettings.SetBgmVolume(v);
        bgmValue.text = Percent(v);
    }

    private void OnSfx(float v)
    {
        GameSettings.SetSfxVolume(v);
        sfxValue.text = Percent(v);
        if (preview != null && preview.clip != null && Time.unscaledTime - lastPreview > 0.15f)
        {
            lastPreview = Time.unscaledTime;
            preview.PlayOneShot(preview.clip);
        }
    }

    private static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";

    private void OnDestroy()
    {
        if (panel != null) panel.transform.DOKill();
    }
}
