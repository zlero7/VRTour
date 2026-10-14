using UnityEngine;

/// <summary>
/// 씬을 넘어가도 남는 투어 진행 상태.
/// 카페(Scene1)에서 고른 메뉴가 약국(Scene3)의 증상 선택지와 결과 화면으로 이어짐.
/// </summary>
public static class TourState
{
    public static string CafeMenu;       // 카페에서 주문한 메뉴 (안 들렀으면 null)
    public static int SymptomIndex = -1; // 그 메뉴 때문에 생긴 증상 (PharmacyGame.symptoms 인덱스)
    public static string Complaint;      // 약국에서 플레이어가 하는 말 ("떡볶이를 먹었더니 목이 칼칼해요")
    public static string Cure;           // 그 말을 들은 약사 대답

    public static bool VisitedCafe => !string.IsNullOrEmpty(CafeMenu);

    public static void SetCafeOrder(string menu, int symptomIndex, string complaint, string cure)
    {
        CafeMenu = menu;
        SymptomIndex = symptomIndex;
        Complaint = complaint;
        Cure = cure;
    }

    // 플레이 시작할 때마다 초기화 (Domain Reload를 꺼둔 경우에도 이전 값이 남지 않게)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        CafeMenu = null;
        SymptomIndex = -1;
        Complaint = null;
        Cure = null;
    }
}
