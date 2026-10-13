using DG.Tweening;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// 계산대(PharmacyCheckout)에 올릴 수 있는 약.
// 손을 가까이 대면 살짝 커지고, 이름표가 위에 따라다니며 항상 플레이어를 바라봄
public class PharmacyItem : MonoBehaviour
{
    public string itemName;
    public Transform label;           // 이름표 (월드에 따로 있음)
    public float labelHeight = 0.15f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(_ => transform.DOScale(baseScale * 1.15f, 0.15f));
            interactable.hoverExited.AddListener(_ => transform.DOScale(baseScale, 0.15f));
        }
    }

    private void LateUpdate()
    {
        if (label == null) return;
        label.position = transform.position + Vector3.up * labelHeight;
        Camera cam = Camera.main;
        if (cam != null) label.rotation = Quaternion.LookRotation(label.position - cam.transform.position);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
