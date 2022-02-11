using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PngTextChunkTest : MonoBehaviour
{
    private struct TextChunkData
    {
        public int length;
        public string chunkType;
        public string keyword;
        public string text;
    }

    [SerializeField] private string _url = "https://cdn-ak.f.st-hatena.com/images/fotolife/e/edo_m18/20220205/20220205121623.png";
    [SerializeField] private string _filename = "downloaded-image";
    [SerializeField] private string _embedText = "This is a sample.";
    [SerializeField] private RawImage _downloadPreview;
    [SerializeField] private RawImage _loadPreview;
    [SerializeField] private Text _textPreview;

    private string FilePath => Path.Combine(Application.persistentDataPath, _filename);
    private Encoding _latin1 = Encoding.GetEncoding(28591);

    #region ### ------------------------------ MonoBehaviour ------------------------------ ###

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            StartDownload();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Load();
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 350, 100), "Download"))
        {
            StartDownload();
        }

        if (GUI.Button(new Rect(10, 120, 350, 100), "Load"))
        {
            Load();
        }
    }

    #endregion ### ------------------------------ MonoBehaviour ------------------------------ ###

    private void Load()
    {
        byte[] data = File.ReadAllBytes(FilePath);

        if (!PngParser.IsPng(data))
        {
            Debug.LogError($"[{nameof(PngTextChunkTest)}] Provided data is not PNG format.");
            return;
        }
        
        Texture2D texture = new Texture2D(0, 0);
        texture.LoadImage(data);
        texture.Apply();

        _loadPreview.texture = texture;

        float ratio = texture.width / (float)texture.height;
        Vector2 size = _loadPreview.rectTransform.sizeDelta;
        float ratio2 = size.x / size.y;
        size.x *= ratio / ratio2;
        _loadPreview.rectTransform.sizeDelta = size;

        if (!TryGetTextChunkData(data, out TextChunkData textChunk))
        {
            return;
        }
        
        _textPreview.text = textChunk.text;
    }

    private bool TryGetTextChunkData(byte[] data, out TextChunkData textChunkData)
    {
        Chunk chunk = PngParser.ParseChunk(data, PngParser.PngHeaderSize);
    
        int separatePosition = -1;
        for (int i = 0; i < chunk.chunkData.Length; ++i)
        {
            if (chunk.chunkData[i] == 0)
            {
                separatePosition = i;
                break;
            }
        }
    
        string keyword = _latin1.GetString(chunk.chunkData, 0, separatePosition);
        string text = _latin1.GetString(chunk.chunkData, separatePosition + 1, chunk.chunkData.Length - separatePosition - 1);
    
        textChunkData = new TextChunkData
        {
            length = chunk.length,
            chunkType = chunk.chunkType,
            keyword = keyword,
            text = text,
        };
    
        return true;
    }

    private void StartDownload()
    {
        StartCoroutine(Download());
    }

    private IEnumerator Download()
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(_url);

        yield return req.SendWebRequest();

        Texture2D tex = DownloadHandlerTexture.GetContent(req);
        _downloadPreview.texture = tex;

        float ratio = tex.width / (float)tex.height;
        Vector2 size = _downloadPreview.rectTransform.sizeDelta;
        float ratio2 = size.x / size.y;
        size.x *= ratio / ratio2;
        _downloadPreview.rectTransform.sizeDelta = size;

        Debug.Log($"Save a texture to [{FilePath}]");

        byte[] data = tex.EncodeToPNG();
        byte[] chunkData = CreateTextChunkData();

        int embededDataSize = data.Length + chunkData.Length;
        byte[] embededData = new byte[embededDataSize];

        // Copy the PNG header to the result.
        Array.Copy(data, 0, embededData, 0, PngParser.PngHeaderSize);

        // Add a tEXT chunk.
        Array.Copy(chunkData, 0, embededData, PngParser.PngHeaderSize, chunkData.Length);

        // Join the data chunks to the result.
        Array.Copy(data, PngParser.PngHeaderSize, embededData, PngParser.PngHeaderSize + chunkData.Length, data.Length - PngParser.PngHeaderSize);

        File.WriteAllBytes(FilePath, embededData);
    }

    private byte[] CreateTextChunkData()
    {
        byte[] chunkTypeData = Encoding.ASCII.GetBytes("tEXt");
        byte[] keywordData = _latin1.GetBytes("Comment");
        byte[] separatorData = new byte[] { 0 };
        byte[] textData = _latin1.GetBytes(_embedText);

        int headerSize = sizeof(byte) * (chunkTypeData.Length + sizeof(int));
        int footerSize = sizeof(byte) * 4; // CRC
        int chunkDataSize = keywordData.Length + separatorData.Length + textData.Length;

        byte[] chunkData = new byte[chunkDataSize];
        Array.Copy(keywordData, 0, chunkData, 0, keywordData.Length);
        Array.Copy(separatorData, 0, chunkData, keywordData.Length, separatorData.Length);
        Array.Copy(textData, 0, chunkData, keywordData.Length + separatorData.Length, textData.Length);

        byte[] lengthData = BitConverter.GetBytes(chunkDataSize);

        uint crc = Crc32.Hash(0, chunkTypeData);
        crc = Crc32.Hash(crc, chunkData);
        byte[] crcData = BitConverter.GetBytes(crc);

        byte[] data = new byte[headerSize + chunkDataSize + footerSize];

        Array.Reverse(lengthData);
        Array.Reverse(crcData);

        Array.Copy(lengthData, 0, data, 0, lengthData.Length);
        Array.Copy(chunkTypeData, 0, data, lengthData.Length, chunkTypeData.Length);
        Array.Copy(chunkData, 0, data, lengthData.Length + chunkTypeData.Length, chunkData.Length);
        Array.Copy(crcData, 0, data, lengthData.Length + chunkTypeData.Length + chunkData.Length, crcData.Length);

        return data;
    }

}