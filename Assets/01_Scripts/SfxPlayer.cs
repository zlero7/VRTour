using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    public AudioSource audioSource; // 같은 오브젝트의 Audio Source를 끌어다 놓기
    public AudioClip sfxClip; // 효과음 파일 끌어다 놓기

    public void Play()
    {
        audioSource.PlayOneShot(sfxClip);
    }
}
