using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스탬프 판: 스탬프마다 칸이 있고 얻은 건 금색, 못 얻은 건 회색으로 보임. 열릴 때마다 새로 그림.
/// </summary>
public class StampBoard : MonoBehaviour
{
    public Image[] cellBackgrounds;
    public TMP_Text[] cellLabels;
    public TMP_Text summary;

    private static readonly Color Got = new Color(1f, 0.85f, 0.4f);
    private static readonly Color NotYet = new Color(0.75f, 0.75f, 0.78f, 0.6f);

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        for (int i = 0; i < cellBackgrounds.Length && i < StampBook.Total; i++)
        {
            bool has = StampBook.Has((Stamp)i);
            cellBackgrounds[i].color = has ? Got : NotYet;
            cellLabels[i].text = has
                ? $"{StampBook.Names[i]}\n획득!"
                : $"{StampBook.Names[i]}\n{StampBook.Hints[i]}";
        }

        string quiz = StampBook.QuizScore >= 0 ? $"  /  퀴즈 {StampBook.QuizScore}/{StampBook.QuizTotal}" : "";
        summary.text = StampBook.IsComplete
            ? $"모든 스탬프를 모았어요! ({StampBook.Count}/{StampBook.Total}){quiz}"
            : $"스탬프 {StampBook.Count}/{StampBook.Total}{quiz}";
    }
}
