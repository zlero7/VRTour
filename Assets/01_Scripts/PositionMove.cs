using UnityEngine;

public class PositionMove : MonoBehaviour
{
    public Transform player;

    public void PlayerTel(Transform target)
    {
        player.position = target.position;
        Debug.Log(target + "의 위치로 이동했습니다.");
    }
}
