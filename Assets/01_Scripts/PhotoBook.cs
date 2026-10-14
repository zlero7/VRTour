using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 찍은 사진 보관소. 사진은 PNG 파일로 저장하고(persistentDataPath/Photos), 최근 사진은 메모리에도 들고 있어
/// 씬을 넘어가도 타이틀의 [내 사진]에서 볼 수 있음.
/// </summary>
public static class PhotoBook
{
    public const int MaxKept = 12;

    public static readonly List<Texture2D> Photos = new List<Texture2D>();
    public static int TotalTaken; // 이번 실행에서 찍은 장수

    public static string Folder => Path.Combine(Application.persistentDataPath, "Photos");

    // 파일로 저장하고 메모리 목록에 추가. 저장된 경로를 돌려줌 (실패하면 null)
    public static string Add(Texture2D tex)
    {
        string path = null;
        try
        {
            Directory.CreateDirectory(Folder);
            path = Path.Combine(Folder, $"photo_{System.DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PhotoBook] 사진 파일 저장 실패: " + e.Message);
            path = null;
        }

        Photos.Add(tex);
        TotalTaken++;
        while (Photos.Count > MaxKept)
        {
            Object.Destroy(Photos[0]);
            Photos.RemoveAt(0);
        }
        return path;
    }

    // 앱을 다시 켠 뒤처럼 메모리에 사진이 없으면, 저장된 파일 중 최근 max장을 불러옴
    public static void LoadFromDisk(int max)
    {
        if (Photos.Count > 0 || !Directory.Exists(Folder)) return;

        var files = Directory.GetFiles(Folder, "*.png").OrderBy(File.GetLastWriteTime).ToArray();
        foreach (string file in files.Skip(Mathf.Max(0, files.Length - max)))
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(File.ReadAllBytes(file))) Photos.Add(tex);
            else Object.Destroy(tex);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        Photos.Clear();
        TotalTaken = 0;
    }
}
