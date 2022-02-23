using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PerformanceCheck : MonoBehaviour
{
    [SerializeField] private string _filename = "downloaded-image";
    [SerializeField] private int _testCount = 100;

    private string FilePath => Path.Combine(Application.persistentDataPath, _filename);

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 30), "Check as normal"))
        {
            TestAsNormal(LoadData());
        }

        if (GUI.Button(new Rect(10, 50, 150, 30), "Check with pointer"))
        {
            TestWithPointer(LoadData());
        }
    }

    private byte[] LoadData()
    {
        return File.ReadAllBytes(FilePath);
    }

    private void TestAsNormal(byte[] data)
    {
        float avg = 0;

        for (int i = 0; i < _testCount; i++)
        {
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            CheckAsNormal(data);
            sw.Stop();

            avg += sw.ElapsedMilliseconds;
        }

        avg /= 100;

        Debug.Log($"[Normal] average time: {avg}");
    }

    private void TestWithPointer(byte[] data)
    {
        float avg = 0;

        for (int i = 0; i < _testCount; i++)
        {
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            CheckWithPointer(data);
            sw.Stop();

            avg += sw.ElapsedMilliseconds;
        }

        avg /= _testCount;

        Debug.Log($"[Pointer] average time: {avg}");
    }

    private static void CheckAsNormal(byte[] rawData)
    {
        (PngMetaData metaData, byte[] data) = PngParser.Decompress(rawData);

        // Profiler.BeginSample("CheckAsNormal");

        Pixel32 zero = Pixel32.Zero;

        for (int y = 0; y < metaData.height; ++y)
        {
            int idx = metaData.rowSize * y;
            int startIndex = idx + 1;

            for (int x = 0; x < metaData.width; ++x)
            {
                Pixel32 pixel = PngParser.GetPixel32(data, startIndex + (x * metaData.stride));
                Pixel32 test = zero + pixel;
            }
        }

        // Profiler.EndSample();
    }

    private static void CheckWithPointer(byte[] rawData)
    {
        (PngMetaData metaData, byte[] data) = PngParser.Decompress(rawData);

        // Profiler.BeginSample("CheckWithPointer");

        Pixel32 zero = Pixel32.Zero;

        unsafe
        {
            fixed (byte* pin = data)
            {
                for (int y = 0; y < metaData.height; ++y)
                {
                    int idx = metaData.rowSize * y;
                    int startIndex = idx + 1;

                    for (int x = 0; x < metaData.width; ++x)
                    {
                        byte* p = pin + startIndex + (x * metaData.stride);
                        Pixel32* pixel = (Pixel32*)p;
                        Pixel32 test = zero + *pixel;
                    }
                }
            }
        }

        // Profiler.EndSample();
    }
}