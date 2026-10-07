using UnityEngine;

public class BgmKeeper : MonoBehaviour
{
    static BgmKeeper instance;

    void Awake()
    {
        // 이미 BGM이 있으면 새로 생긴 것은 지운다 (타 이틀로 돌아올 때 중복 방지)
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
