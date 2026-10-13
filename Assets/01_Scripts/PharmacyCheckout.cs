using UnityEngine;

// 계산대 위 트리거: 약(PharmacyItem)이 들어오면 PharmacyGame에 알려줌
public class PharmacyCheckout : MonoBehaviour
{
    public PharmacyGame game;

    private void OnTriggerEnter(Collider other)
    {
        var item = other.GetComponentInParent<PharmacyItem>();
        if (item != null && game != null) game.OnItemPlaced(item);
    }
}
