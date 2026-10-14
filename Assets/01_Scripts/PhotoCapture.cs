using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// VR 사진 촬영: 컨트롤러 A(X) 버튼(키보드 P)을 누르면 지금 보이는 장면을 찍어
/// 번쩍임 + 셔터음과 함께 폴라로이드 사진이 눈앞에 뜨고, PNG 파일로 저장됨.
/// 그립은 물건 잡기/NPC 말 걸기에 쓰이고 있어서 겹치지 않는 A 버튼을 사용.
/// </summary>
public class PhotoCapture : MonoBehaviour
{
    public CanvasGroup flash;          // 번쩍임 (눈앞의 흰 판)
    public GameObject card;            // 폴라로이드 카드
    public RawImage cardImage;
    public TMP_Text cardCaption;
    public AudioSource sfx;
    public int width = 1280;
    public int height = 720;
    public float fieldOfView = 70f;

    private InputAction shoot;
    private AudioClip shutterClip;
    private Sequence seq;

    private void Awake()
    {
        card.SetActive(false);
        flash.alpha = 0f;
        shutterClip = MakeShutterClip();
    }

    private void OnEnable()
    {
        shoot = new InputAction("Photo", InputActionType.Button);
        shoot.AddBinding("<XRController>{RightHand}/{PrimaryButton}");
        shoot.AddBinding("<XRController>{LeftHand}/{PrimaryButton}");
        shoot.AddBinding("<Keyboard>/p");
        shoot.performed += OnShoot;
        shoot.Enable();
    }

    private void OnDisable()
    {
        if (shoot == null) return;
        shoot.performed -= OnShoot;
        shoot.Disable();
        shoot.Dispose();
        shoot = null;
    }

    private void OnShoot(InputAction.CallbackContext _) => Take();

    public void Take()
    {
        Camera main = Camera.main;
        if (main == null) return;

        card.SetActive(false); // 이전 사진 카드가 새 사진에 찍히지 않게

        Texture2D photo = Capture(main);
        string path = PhotoBook.Add(photo);
        if (path != null) Debug.Log("[PhotoCapture] 사진 저장: " + path);

        PlayShutter();
        ShowCard(photo);
    }

    // 머리 카메라와 같은 위치·방향의 별도 카메라로 찍음 (XR 카메라는 직접 RenderTexture에 못 그림)
    private Texture2D Capture(Camera main)
    {
        var go = new GameObject("PhotoCam");
        var cam = go.AddComponent<Camera>();
        cam.CopyFrom(main);
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        cam.ResetProjectionMatrix();
        cam.fieldOfView = fieldOfView;
        cam.aspect = (float)width / height;
        cam.enabled = false;
        go.AddComponent<UniversalAdditionalCameraData>();
        go.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);

        var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Destroy(go);
        return tex;
    }

    private void PlayShutter()
    {
        if (sfx != null)
        {
            sfx.pitch = 1f;
            sfx.PlayOneShot(shutterClip);
        }
        flash.DOKill();
        flash.alpha = 0.9f;
        flash.DOFade(0f, 0.4f);
    }

    private void ShowCard(Texture2D photo)
    {
        cardImage.texture = photo;
        cardCaption.text = $"사진 저장 완료 ({PhotoBook.TotalTaken}장)";

        seq?.Kill();
        card.SetActive(true);
        card.transform.localScale = Vector3.one * 0.4f;
        seq = DOTween.Sequence();
        seq.Append(card.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        seq.AppendInterval(3f);
        seq.Append(card.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        seq.OnComplete(() => card.SetActive(false));
    }

    // 찰칵 소리: 짧은 잡음 두 번(셔터 열림/닫힘)을 코드로 만듦
    private static AudioClip MakeShutterClip()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.2f);
        var data = new float[n];
        var rng = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = 0f;
            if (t < 0.05f) env += Mathf.Exp(-t * 90f);
            if (t > 0.09f && t < 0.2f) env += 0.8f * Mathf.Exp(-(t - 0.09f) * 80f);
            data[i] = ((float)rng.NextDouble() * 2f - 1f) * env * 0.7f;
        }
        var clip = AudioClip.Create("Shutter", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        seq?.Kill();
        if (flash != null) flash.DOKill();
    }
}
