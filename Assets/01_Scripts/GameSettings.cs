using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>
/// 설정 값(배경음/효과음 볼륨, 연속 이동 켜기/끄기)을 PlayerPrefs에 저장하고 씬이 로드될 때마다 적용.
/// BgmKeeper가 붙은 AudioSource는 배경음, 나머지 AudioSource는 효과음으로 취급.
/// </summary>
public static class GameSettings
{
    private const string BgmKey = "Settings.BgmVolume";
    private const string SfxKey = "Settings.SfxVolume";
    private const string MoveKey = "Settings.ContinuousMove";

    public static float BgmVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    // 한 번도 안 건드렸으면 씬에 설정된 값을 그대로 둠
    public static bool ContinuousMove { get; private set; } = true;

    public static event System.Action Changed;

    // AudioSource별 원래 볼륨 (슬라이더 값은 이 값에 곱함)
    private static readonly Dictionary<AudioSource, float> baseVolume = new Dictionary<AudioSource, float>();
    private static bool moveOverridden;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        BgmVolume = PlayerPrefs.GetFloat(BgmKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
        moveOverridden = PlayerPrefs.HasKey(MoveKey);
        ContinuousMove = PlayerPrefs.GetInt(MoveKey, 1) == 1;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

    public static void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BgmKey, BgmVolume);
        Apply();
        Changed?.Invoke();
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        Apply();
        Changed?.Invoke();
    }

    public static void SetContinuousMove(bool on)
    {
        ContinuousMove = on;
        moveOverridden = true;
        PlayerPrefs.SetInt(MoveKey, on ? 1 : 0);
        Apply();
        Changed?.Invoke();
    }

    public static void Apply()
    {
        var stale = new List<AudioSource>();
        foreach (var kv in baseVolume) if (kv.Key == null) stale.Add(kv.Key);
        foreach (var s in stale) baseVolume.Remove(s);

        foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!baseVolume.TryGetValue(source, out float basis))
            {
                basis = source.volume;
                baseVolume[source] = basis;
            }
            bool isBgm = source.GetComponent<BgmKeeper>() != null;
            source.volume = basis * (isBgm ? BgmVolume : SfxVolume);
        }

        if (moveOverridden)
        {
            foreach (var move in Object.FindObjectsByType<ContinuousMoveProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                move.enabled = ContinuousMove;
        }
    }
}
