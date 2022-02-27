using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImagePreview : MonoBehaviour
{
    public event System.Action<string> OnClicked;
    
    [SerializeField] private RawImage _preview;
    [SerializeField] private Button _button;
    [SerializeField] private float _baseSize = 100f;

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

        float ratio = texture.height / (float)texture.width;
        float height = (ratio * _baseSize) - _baseSize;
        _preview.rectTransform.sizeDelta = new Vector2(0, height);
    }
}