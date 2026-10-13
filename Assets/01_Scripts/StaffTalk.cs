using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 직원(NPC)에게 말 걸기: 컨트롤러로 NPC를 잡거나(Select) 옆의 "말 걸기" 버튼을 누르면 Talked 이벤트 발생.
/// 손이나 레이가 닿으면 살짝 커져서 말을 걸 수 있다는 걸 알려줌.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class StaffTalk : MonoBehaviour
{
    public Button talkButton; // 머리 옆 "말 걸기" 버튼 (트리거로 누름)

    public event System.Action Talked;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
        var interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(_ => Talk());
        interactable.hoverEntered.AddListener(_ => transform.DOScale(baseScale * 1.05f, 0.15f));
        interactable.hoverExited.AddListener(_ => transform.DOScale(baseScale, 0.15f));
        if (talkButton != null) talkButton.onClick.AddListener(Talk);
    }

    public void Talk() => Talked?.Invoke();

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
