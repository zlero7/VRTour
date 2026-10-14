using UnityEngine;

/// <summary>
/// 씬에 들어오면 잠깐 뒤에 스탬프를 줌 (페이드 인이 끝난 뒤에 알림이 보이도록 지연).
/// </summary>
public class StampTrigger : MonoBehaviour
{
    public Stamp stamp = Stamp.Street;
    public float delay = 1.2f;

    private void Start() => Invoke(nameof(Give), delay);

    private void Give() => StampBook.Collect(stamp);
}
