using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 스탬프를 얻으면 시야 위쪽에 "스탬프 획득!" 알림이 잠깐 떴다 사라짐 (머리에 붙은 캔버스).
/// </summary>
public class StampToast : MonoBehaviour
{
    public GameObject panel;
    public CanvasGroup group;
    public TMP_Text label;
    public AudioSource sfx;

    private Sequence seq;

    private void Awake() => panel.SetActive(false);

    private void OnEnable() => StampBook.Collected += Show;

    private void OnDisable() => StampBook.Collected -= Show;

    private void Show(Stamp stamp)
    {
        label.text = $"스탬프 획득! {StampBook.Names[(int)stamp]}\n({StampBook.Count}/{StampBook.Total})";

        if (sfx != null && sfx.clip != null)
        {
            sfx.pitch = 1.5f;
            sfx.PlayOneShot(sfx.clip);
        }

        seq?.Kill();
        panel.SetActive(true);
        group.alpha = 0f;
        panel.transform.localScale = Vector3.one * 0.8f;

        seq = DOTween.Sequence();
        seq.Append(group.DOFade(1f, 0.25f));
        seq.Join(panel.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
        seq.AppendInterval(2.2f);
        seq.Append(group.DOFade(0f, 0.4f));
        seq.OnComplete(() => panel.SetActive(false));
    }

    private void OnDestroy() => seq?.Kill();
}
