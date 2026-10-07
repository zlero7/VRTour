using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMove : MonoBehaviour
{
    public void SceneMovement(string scenename)
    {
        // 이동하기 전에 현재 씬 이름을 "LastScene"이라는 이름으로 기억해 둡니다.
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        // 페이드 아웃 → 씬 로드 → 페이드 인 (VRSceneFader)
        VRSceneFader.LoadScene(scenename);
    }

    // 버튼에 이 함수를 연결하면 이전 씬으로 돌아갑니다.
    public void PrevScene()
    {
        // 저장되어 있던 이전 씬 이름을 가져옵니다. (기록이 없다면 "Main"으로 이동)
        string lastSceneName = PlayerPrefs.GetString("LastScene", "Main");
        VRSceneFader.LoadScene(lastSceneName);
    }
}