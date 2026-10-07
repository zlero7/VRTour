using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// VR 씬 전환 자동 페이드 (DOTween).
/// - 씬에 아무것도 안 넣어도 게임 시작 시 자동으로 생성됨
/// - 어떤 방식으로 씬이 로드되든(SceneManager.LoadScene 등) 감지해서 검은 화면에서 페이드 인
/// - 페이드 아웃까지 원하면 VRSceneFader.LoadScene("씬이름") 사용 (SceneMove가 사용 중)
/// </summary>
public class VRSceneFader : MonoBehaviour
{
    // ===== 설정 (필요하면 여기 값만 수정) =====
    private const float FadeInTime = 0.8f;
    private const float FadeOutTime = 0.5f;      // LoadScene() 쓸 때만 사용
    private const float FadeInDelay = 0.2f;      // 씬 로드 직후 끊김을 가리려고 검은 화면 유지
    private const Ease FadeEase = Ease.InOutSine;
    private static readonly Color FadeColor = Color.black;
    private const float NearClipMultiplier = 2f; // 카메라 Near Clip의 몇 배 거리에 둘지 (1보다 커야 함)
    private const float QuadSizePerDistance = 10f; // 거리 대비 Quad 크기 (시야 전체를 덮도록)
    private const bool FadeInOnFirstScene = true;
    // =========================================

    private static VRSceneFader instance;

    private Renderer quadRenderer;
    private Material mat;
    private Transform quadTr;
    private Tween fadeTween;
    private bool isLoading;
    private bool firstSceneHandled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        var go = new GameObject("[VRSceneFader]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<VRSceneFader>();
    }

    private void Awake()
    {
        CreateQuad();
        SceneManager.sceneLoaded += OnSceneLoaded;
        // XR 헤드 트래킹은 LateUpdate 이후에 갱신되므로 렌더 직전에 위치를 맞춰야 흔들리지 않음
        Application.onBeforeRender += FollowCamera;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.onBeforeRender -= FollowCamera;
        fadeTween?.Kill();
        if (mat != null) Destroy(mat);
    }

    private void CreateQuad()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "FadeQuad";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        quadTr = go.transform;

        // UI/Default는 ZTest를 바꿀 수 있음 → Always로 두면 손/컨트롤러보다 항상 위에 그려짐
        mat = new Material(Shader.Find("UI/Default")) { renderQueue = 5000 };
        mat.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        quadRenderer = go.GetComponent<Renderer>();
        quadRenderer.sharedMaterial = mat;
        quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;

        SetAlpha(0f);
    }

    private void FollowCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float distance = cam.nearClipPlane * NearClipMultiplier;
        Transform camTr = cam.transform;
        quadTr.SetPositionAndRotation(camTr.position + camTr.forward * distance, camTr.rotation);
        quadTr.localScale = new Vector3(distance * QuadSizePerDistance, distance * QuadSizePerDistance, 1f);
    }

    // 씬이 로드될 때마다 자동 호출 (첫 씬 포함)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Additive 로드는 화면 전환이 아니므로 무시
        if (mode == LoadSceneMode.Additive) return;

        bool isFirstScene = !firstSceneHandled;
        firstSceneHandled = true;
        isLoading = false;

        if (isFirstScene && !FadeInOnFirstScene) return;

        FollowCamera();
        SetAlpha(1f);
        FadeTo(0f, FadeInTime, FadeInDelay);
    }

    /// <summary>페이드 아웃 → 씬 로드. 로드 후 페이드 인은 자동.</summary>
    public static void LoadScene(string sceneName)
    {
        if (instance == null) Bootstrap();
        if (instance.isLoading) return;

        instance.isLoading = true;
        instance.FadeTo(1f, FadeOutTime, 0f, () => SceneManager.LoadSceneAsync(sceneName));
    }

    private void FadeTo(float targetAlpha, float duration, float delay, TweenCallback onComplete = null)
    {
        fadeTween?.Kill();
        quadRenderer.enabled = true;

        fadeTween = mat.DOFade(targetAlpha, duration)
            .SetDelay(delay)
            .SetEase(FadeEase)
            .SetUpdate(true) // timeScale 무시
            .OnComplete(() =>
            {
                quadRenderer.enabled = targetAlpha > 0.001f;
                onComplete?.Invoke();
            });
    }

    private void SetAlpha(float a)
    {
        Color c = FadeColor;
        c.a = a;
        mat.color = c;
        quadRenderer.enabled = a > 0.001f;
    }
}