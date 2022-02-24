using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PngImageManager : MonoBehaviour
{
    [SerializeField] private InputField _urlField;
    [SerializeField] private Button _downLoadButton;
    // [SerializeField] private Button _loadButton;
    
    private Encoding _latin1 = Encoding.GetEncoding(28591);

    private void Awake()
    {
        _downLoadButton.onClick.AddListener(() =>
        {
            DownloadAsync(_urlField.text).Forget();
        });

        // _loadButton.clicked += () =>
        // {
        //     Load(_urlField.text).Forget();
        // };
    }

    // public static async UniTask<Texture2D> Load(string url, CancellationToken token = default)
    // {
    //     string filePath = GetSavePath(url);
    //     byte[] data = File.ReadAllBytes(filePath);
    //
    //     if (!PngParser.IsPng(data))
    //     {
    //         Debug.LogError($"[{nameof(PngTextChunkTest)}] Provided data is not PNG format.");
    //         return null;
    //     }
    //
    //     return await PngParser.Parse(data, token);
    // }

    public static async UniTask<Texture2D> DownloadAsync(string url)
    {
        string savePath = GetSavePath(url);
        // if (File.Exists(savePath))
        // {
        //     return await Load(url);
        // }
        //
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);

        await req.SendWebRequest();

        Texture2D texture = DownloadHandlerTexture.GetContent(req);

        Debug.Log($"Save a texture to [{savePath}]");

        byte[] data = texture.EncodeToPNG();
        File.WriteAllBytes(savePath, data);

        return texture;
    }

    public static string GetSavePath(string url)
    {
        byte[] binary = Encoding.UTF8.GetBytes(url);
        MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
        byte[] hashBytes = csp.ComputeHash(binary);
        StringBuilder builder = new StringBuilder();
        foreach (var b in hashBytes)
        {
            builder.Append(b.ToString("x2"));
        }

        string filename = builder.ToString();

        return Path.Combine(Application.persistentDataPath, filename);
    }
}