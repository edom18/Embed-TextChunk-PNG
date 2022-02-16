using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class CopyStressTest : MonoBehaviour
{
    [SerializeField] private int _dataCount = 5000000;
    [SerializeField] private int _testCount = 1000;

    private void Update()
    {
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 200, 100), "Perform with normal array"))
        {
            TestNormal();
        }

        if (GUI.Button(new Rect(10, 120, 200, 100), "Perform with pointer"))
        {
            TestPointer();
        }
    }

    private byte[] CreateDataAlign4byte(int size)
    {
        int newSize = (size / 4) * 4;

        byte[] data = new byte[newSize];

        for (int i = 0; i < newSize; i++)
        {
            data[i] = (byte)(i % 256);
        }

        return data;
    }

    private void TestNormal()
    {
        Assert.IsTrue(_dataCount >= 4);

        byte[] data = CreateDataAlign4byte(_dataCount);

        Stopwatch sw = new Stopwatch();
        float avg = 0;

        for (int t = 0; t < _testCount; t++)
        {
            sw.Restart();
            for (int i = 0; i < data.Length; i += 4)
            {
                Pixel32 a = new Pixel32(2, 2, 2, 2);
                Pixel32 b = new Pixel32(
                    data[i + 0],
                    data[i + 1],
                    data[i + 2],
                    data[i + 3]);

                a += b;

                data[i + 0] = a.r;
                data[i + 1] = a.g;
                data[i + 2] = a.b;
                data[i + 3] = a.a;
            }

            sw.Stop();

            avg += sw.ElapsedMilliseconds;
        }

        avg /= _testCount;

        Debug.Log($"[Normal] Elapsed time average : {avg.ToString()}ms");
    }

    private unsafe void TestPointer()
    {
        byte[] data = CreateDataAlign4byte(_dataCount);

        Stopwatch sw = new Stopwatch();

        float avg = 0;

        fixed (byte* pin = data)
        {
            for (int t = 0; t < _testCount; t++)
            {
                sw.Restart();
                byte* p = pin;
                byte* last = p + data.Length;

                while (p + 3 < last)
                {
                    Pixel32 a = new Pixel32(2, 2, 2, 2);
                    Pixel32* b = (Pixel32*)p;

                    a += *b;

                    *(uint*)p = *(uint*)&a;

                    p += 4;
                }

                sw.Stop();
                avg += sw.ElapsedMilliseconds;
            }
        }

        avg /= _testCount;

        Debug.Log($"[Pointer] Elapsed time average : {avg.ToString()}ms");
    }
}