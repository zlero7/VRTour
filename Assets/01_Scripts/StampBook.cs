using UnityEngine;

public enum Stamp { Street, Cafe, Quiz, Pharmacy }

/// <summary>
/// 투어 스탬프 수집 상태. 씬을 넘어가도 유지되고, 앱을 새로 시작하면 초기화됨.
/// 스탬프를 처음 얻을 때만 Collected 이벤트가 발생 (StampToast가 알림을 띄움).
/// </summary>
public static class StampBook
{
    public static readonly string[] Names = { "골목 도착", "카페 주문", "퀴즈 풀이", "약국 완료" };
    public static readonly string[] Hints = { "먹자골목에 가보기", "카페에서 메뉴 주문하기", "퀴즈 2문제 이상 맞히기", "약국에서 약 사기" };

    private static readonly bool[] got = new bool[Names.Length];

    public static int QuizScore = -1; // 가장 잘 본 퀴즈 점수 (안 풀었으면 -1)
    public static int QuizTotal;

    public static event System.Action<Stamp> Collected;

    public static int Total => got.Length;
    public static int Count
    {
        get
        {
            int n = 0;
            foreach (bool g in got) if (g) n++;
            return n;
        }
    }
    public static bool IsComplete => Count == Total;

    public static bool Has(Stamp stamp) => got[(int)stamp];

    public static bool Collect(Stamp stamp)
    {
        if (got[(int)stamp]) return false;
        got[(int)stamp] = true;
        Collected?.Invoke(stamp);
        return true;
    }

    public static void ResetAll()
    {
        for (int i = 0; i < got.Length; i++) got[i] = false;
        QuizScore = -1;
        QuizTotal = 0;
    }

    // 플레이 시작할 때마다 초기화 (Domain Reload를 꺼둔 경우에도 이전 값이 남지 않게)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        ResetAll();
        Collected = null;
    }
}
