using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀의 [내 사진]: 최근에 찍은 사진 3장을 보여줌. 열릴 때마다 새로 그림.
/// </summary>
public class PhotoGallery : MonoBehaviour
{
    public RawImage[] slots;     // 사진이 들어갈 칸 (왼쪽이 더 오래된 사진)
    public TMP_Text emptyText;   // 사진이 하나도 없을 때 안내

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        PhotoBook.LoadFromDisk(slots.Length);

        int count = PhotoBook.Photos.Count;
        int shown = Mathf.Min(slots.Length, count);
        for (int i = 0; i < slots.Length; i++)
        {
            bool has = i < shown;
            slots[i].enabled = has;
            if (has) slots[i].texture = PhotoBook.Photos[count - shown + i];
        }
        emptyText.gameObject.SetActive(count == 0);
    }
}
