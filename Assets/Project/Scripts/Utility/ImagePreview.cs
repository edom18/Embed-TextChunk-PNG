using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImagePreview : MonoBehaviour
{
    public event System.Action<string> OnClicked;
    
    [SerializeField] private RawImage _preview;
    [SerializeField] private Button _button;

    private string _filePath;

    private void Awake()
    {
        _button.onClick.AddListener(() =>
        {
            OnClicked?.Invoke(_filePath);
        });
    }

    public void LoadImage(string filePath)
    {
        _filePath = filePath;

        byte[] data = File.ReadAllBytes(_filePath);
        Texture2D texture = new Texture2D(0, 0);
        texture.LoadImage(data);
        _preview.texture = texture;
    }
}