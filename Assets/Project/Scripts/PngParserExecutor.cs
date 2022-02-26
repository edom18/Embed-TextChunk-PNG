using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class PngParserExecutor : MonoBehaviour
{
    [SerializeField] private InputField _urlField;
    [SerializeField] private RawImage _preview;
    [SerializeField] private Button _loadButton;

    private JobHandle _jobHandle;
    private Stopwatch _stopwatch;
    private bool _started = false;

    private NativeArray<int> _type1Indices;
    private NativeArray<int> _otherIndices;
    private NativeArray<byte> _dataArray;
    private NativeArray<Pixel32> _pixelArray;
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
            indices = _type1Indices,
            data = _dataArray,
            pixels = _pixelArray,
            metaData = _metaData,
        };

        ExpandJob job = new ExpandJob
        {
            indices = _otherIndices,
            data = _dataArray,
            pixels = _pixelArray,
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

        IntPtr pointer = (IntPtr)_pixelArray.GetUnsafePtr();

        Texture2D texture = new Texture2D(_metaData.width, _metaData.height, TextureFormat.RGBA32, false);

        texture.LoadRawTextureData(pointer, _metaData.width * _metaData.height * 4);
        texture.Apply();

        _preview.texture = texture;

        Dispose();

        _started = false;

        _stopwatch.Stop();

        Debug.Log($"Elapsed time: {_stopwatch.ElapsedMilliseconds.ToString()}ms");
    }

    private void Dispose()
    {
        _type1Indices.Dispose();
        _otherIndices.Dispose();
        _dataArray.Dispose();
        _pixelArray.Dispose();
    }
}