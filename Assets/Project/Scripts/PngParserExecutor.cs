using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class PngParserExecutor : MonoBehaviour
{
    [SerializeField] private RawImage _preview;
    [SerializeField] private TMP_Text _logText;

    private JobHandle _jobHandle;
    private Stopwatch _stopwatch;
    private bool _started = false;

    private NativeArray<int>? _type1Indices;
    private NativeArray<int>? _otherIndices;
    private NativeArray<byte>? _dataArray;
    private NativeArray<Pixel32>? _pixelArray;
    private PngMetaData _metaData;

    private void Start()
    {
        _stopwatch = new Stopwatch();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    public async void LoadImage(string filePath)
    {
        if (_started)
        {
            return;
        }

        _started = true;

        await PrepareJob(filePath);
        
        ShowResult();
    }

    private async Task PrepareJob(string filePath)
    {
        (PngMetaData metaData, byte[] data) = await Task.Run(() =>
        {
            byte[] rawData = File.ReadAllBytes(filePath);
            return PngParser.Decompress(rawData);
        });
        
        _metaData = metaData;
        LineInfo info = PngParser.ExtractLines(data, _metaData);

        _type1Indices = new NativeArray<int>(info.filterType1, Allocator.Persistent);
        _otherIndices = new NativeArray<int>(info.otherType, Allocator.Persistent);
        _dataArray = new NativeArray<byte>(data, Allocator.Persistent);
        _pixelArray = new NativeArray<Pixel32>(_metaData.width * _metaData.height, Allocator.Persistent);
        
        _stopwatch.Restart();

        ExpandType1Job type1Job = new ExpandType1Job
        {
            indices = _type1Indices.Value,
            data = _dataArray.Value,
            pixels = _pixelArray.Value,
            metaData = _metaData,
        };

        ExpandJob job = new ExpandJob
        {
            indices = _otherIndices.Value,
            data = _dataArray.Value,
            pixels = _pixelArray.Value,
            metaData = _metaData,
        };

        JobHandle type1JobHandle = type1Job.Schedule(info.filterType1.Length, 32);
        _jobHandle = job.Schedule(type1JobHandle);
    }

    private unsafe void ShowResult()
    {
        // Needs to complete even if it checked `IsCompleted`.
        // This just avoids an error.
        _jobHandle.Complete();

        if (!_pixelArray.HasValue)
        {
            Debug.LogWarning($"{nameof(_pixelArray)} is already disposed or null.");
            return;
        }
        
        IntPtr pointer = (IntPtr)_pixelArray.Value.GetUnsafePtr();

        Texture2D texture = new Texture2D(_metaData.width, _metaData.height, TextureFormat.RGBA32, false);

        texture.LoadRawTextureData(pointer, _metaData.width * _metaData.height * 4);
        texture.Apply();

        float ratio = texture.width / (float)texture.height;
        Vector2 size = _preview.rectTransform.sizeDelta;
        float ratio2 = size.x / size.y;
        size.x *= ratio / ratio2;
        _preview.rectTransform.sizeDelta = size;
        _preview.texture = texture;

        Dispose();

        _started = false;

        _stopwatch.Stop();

        Debug.Log($"Elapsed time: {_stopwatch.ElapsedMilliseconds.ToString()}ms");
        
        _logText.text = $"{_stopwatch.ElapsedMilliseconds.ToString()} ms";
    }

    private void Dispose()
    {
        _type1Indices?.Dispose();
        _otherIndices?.Dispose();
        _dataArray?.Dispose();
        _pixelArray?.Dispose();

        _type1Indices = null;
        _otherIndices = null;
        _dataArray = null;
        _pixelArray = null;
    }
}